using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.ThirdParty;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Newtonsoft.Json;
using System.ComponentModel;
using Hangfire.Server;
using Hangfire;
using System.Net;
using HangfireService.Core.Helpers;
using HangfireService.Core.Models.Options;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace HangfireService.Core.Jobs
{
    public class SMSCheckBlanceJob : BaseJob, IScheduledJob
    {
        private readonly MainDBDataLayer _mainDBDataLayer;

        private readonly ThirdPartyManager _thirdPartyManager;
        protected readonly HttpHelper _httpHelper;
        private readonly SecureHelper _secureHelper;
        private readonly IConfiguration _IConfiguration;
        private readonly Dictionary<string, SMSBlanceCheckOption> _smsConfig;
        private readonly Dictionary<string, string> _LetsTalkConfig;

        public SMSCheckBlanceJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
            _thirdPartyManager = provider.GetRequiredService<ThirdPartyManager>();
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _secureHelper = provider.GetRequiredService<SecureHelper>();
            _IConfiguration = provider.GetRequiredService<IConfiguration>();
            _smsConfig = _IConfiguration.GetSection("SMSConfig").Get<Dictionary<string, SMSBlanceCheckOption>>();
            _LetsTalkConfig = _IConfiguration.GetSection("LetsTalkConfig").Get<Dictionary<string, string>>();
        }

        
        public async Task<bool> NxcloudGetBlance(SMSBlanceCheckOption smsOption)
        {
            string result = "fail";
            try
            {
                string ResourceURL = smsOption.URL;
                var data = new Dictionary<string, string>()
                {
                    {"appkey", smsOption.KEY1},
                    {"secretkey", smsOption.KEY2},
                };

                using var httpResponse = await _httpHelper.HttpFromPostAsync(ResourceURL, null, data, "application/x-www-form-urlencoded");
                if (httpResponse != null && httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var responsjson = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                    if (responsjson.TryGetValue("code", out var code))
                    {
                        responsjson.TryGetValue("balance", out var balance);
                        responsjson.TryGetValue("credit_balance", out var credit_balance);
                        if (code.Equals("0"))
                        {
                            result = JsonConvert.SerializeObject(new
                            {
                                code = code,
                                balance = balance,
                                credit_balance = credit_balance
                            });
                            _logger.LogError($"TPName:{smsOption.SMSTPName} Accountname : {smsOption.Accountname} balance : {balance} Minimumlimit : {smsOption.Minimumlimit}");
                            if ((int)Convert.ToDecimal(balance) < smsOption.Minimumlimit)
                            {
                                var msg = MakeAlertMessage(smsOption, balance.ToString());
                                await SandLesTalkMessage(msg);
                            }
                            //sucess
                            return true;
                        }else
                        {
                            _logger.LogError($"NxcloudGetBlance Result error URL : {ResourceURL} responsjson: {responsjson} content : {content}");
                        }
                        
                    }
                    else
                    {
                        _logger.LogError($"httpResponse error URL : {ResourceURL} content : {content}");
                    }
                }
                else
                {
                    _logger.LogError($"httpResponse error URL : {ResourceURL} data:{data} StatusCode: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            return false;

        }
        public string MakeSignString(string account, string pwd, string datatime)
        {
            string tmpstr = $"{account}{pwd}{datatime}";
            return _secureHelper.ToMD5(tmpstr);

        }
        public async Task<bool> SkyLineGetBlance(SMSBlanceCheckOption smsOption)
        {
            string account = smsOption.KEY1;
            string passWord = smsOption.KEY2;
            string urlGetBlance = smsOption.URL;
            string dateTime =  DateTime.UtcNow.AddHours(8).ToString("yyyyMMddHHmmss");
            string sign = MakeSignString(account, passWord, dateTime);
            string url = $"{urlGetBlance}?account={account}&sign={sign}&datetime={dateTime}";

            var data = new Dictionary<string, string>()
                {
                    {"account", account},
                    {"sign", sign},
                    {"datetime", dateTime},
                };
            try
            {
                using var result = await _httpHelper.HttpPost1Async(url, null ,"", "application/json");
                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var ContentResult = result.Content.ReadAsStringAsync().Result;
                    var resultResp = JsonConvert.DeserializeObject<SKYLineSMSResult>(ContentResult);
                    if (resultResp.status == 0)
                    {
                        _logger.LogError($"TPName:{smsOption.SMSTPName} Accountname : {smsOption.Accountname} balance : {resultResp.balance} Minimumlimit : {smsOption.Minimumlimit}");
                        if ((int)Convert.ToDecimal(resultResp.balance) < smsOption.Minimumlimit)
                        {
                            var msg = MakeAlertMessage(smsOption, resultResp.balance);
                            await SandLesTalkMessage(msg);
                        }
                        return true;
                    }
                    else
                    {
                        _logger.LogError($"httpResponse error URL : {url}  resultResp: {ContentResult}");
                    }
                }
                else
                {
                    _logger.LogError($"httpResponse error URL : {url}  StatusCode: {result.StatusCode}");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            return false;
        }
        public string MakeAlertMessage(SMSBlanceCheckOption smsOption, string blance)
        {
            string message =
                $"* SMS Blance Alert * \r\n" +
                $"* Time : {DateTime.UtcNow.AddHours(8).ToString("yyyy-MM-dd HH:mm:ss")} * \r\n" +
                $"=============================\r\n" +
                $"The balance of SMS is less than {smsOption.Minimumlimit}.\r\n" +
                $"Please add value.\r\n" +
                $"=============================\r\n" +
                $"TPName : {smsOption.SMSTPName}\r\n" +
                $"Accountname : {smsOption.Accountname}\r\n" +
                $"Now Blance : {blance}\r\n"
               ;
            return message;
        }
        public async Task<bool> SandLesTalkMessage(string Message)
        {
            string troken = await GetLetsTalkToken();

            var data = new LetsTalkMsgParam();
            data.msg = new LetsTalkMsgtext();
            data.receiver = _LetsTalkConfig["SandMessageRoomID"];
            data.msg.type = "text";
            data.msg.text = Message;

            Dictionary<string, string> headerParams = new Dictionary<string, string> {
                { "Authorization", $"Bearer {troken}"}
            };
            try
            {
                var parameters = JsonConvert.SerializeObject(data);
                using var result = await _httpHelper.HttpPost1Async(_LetsTalkConfig["SandMessageURL"], headerParams, parameters, "application/json");

                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var resultResp = JsonConvert.DeserializeObject<LetsTalkSignResult>(result.Content.ReadAsStringAsync().Result);
                    if (resultResp.status == 1)
                    {
                        _logger.LogError($"SandLesTalkMessage : {parameters}");
                        return true;
                    }
                    else
                    {
                        _logger.LogError($"httpresult error URL : {_LetsTalkConfig["SandMessageURL"]} resultResp:{resultResp}");
                    }
                }
                else
                {
                    _logger.LogError($"httpResponse error URL : {_LetsTalkConfig["SandMessageURL"]} headerParams:{headerParams} parameters:{parameters} StatusCode: {result.StatusCode}");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            return false;

        }
        public async Task<string> GetLetsTalkToken()
        {
            string troken = null;

            var data = new Dictionary<string, string>()
                {
                    {"id", _LetsTalkConfig["SignInId"]},
                    {"token", _LetsTalkConfig["SignInToken"]}
                };
            try
            {
                var parameters = JsonConvert.SerializeObject(data);
                using var result = await _httpHelper.HttpPost1Async(_LetsTalkConfig["SignInURL"], null, parameters, "application/json");

                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var ContentResult = result.Content.ReadAsStringAsync().Result;
                    var resultResp = JsonConvert.DeserializeObject<LetsTalkSignResult>(ContentResult);
                   
                    if (resultResp.status == 1)
                    {
                        return resultResp.token;
                    }
                    else
                    {
                        _logger.LogError($"httpResult error URL : {_LetsTalkConfig["SignInURL"]} Result:{ContentResult} ");
                    }
                }
                else
                {
                    _logger.LogError($"httpResponse error URL : {_LetsTalkConfig["SignInURL"]} parameters:{parameters} StatusCode: {result.StatusCode}");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return troken;

        }
        

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");
            string result = "true";

            foreach (var item in _smsConfig)
            {
                if (item.Value.SMSTPName.Equals("SkyLine"))
                {
                    await SkyLineGetBlance(item.Value);
                }
                else
                {
                    await NxcloudGetBlance(item.Value);
                }
            }
            return result;

        }
    }
}
