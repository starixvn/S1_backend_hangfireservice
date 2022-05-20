using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
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

namespace HangfireService.Core.Jobs
{
    public class SMSCheckBalanceJob : BaseJob, IScheduledJob
    {
        protected readonly HttpHelper _httpHelper;
        private readonly SecureHelper _secureHelper;
        private readonly Dictionary<string, SMSBlanceCheckOption> _smsConfig;
        private readonly string _letsTalkGroupName;
        private readonly string _commonServiceHost;

        public SMSCheckBalanceJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _secureHelper = provider.GetRequiredService<SecureHelper>();
            var iConfiguration = provider.GetRequiredService<IConfiguration>();
            _smsConfig = iConfiguration.GetSection("SMSConfig").Get<Dictionary<string, SMSBlanceCheckOption>>();
            _letsTalkGroupName = iConfiguration.GetValue<string>("LetsTalkConfig:LetsTalkGroupName");
            _commonServiceHost = iConfiguration.GetValue<string>("AiLabServiceHost:CommonServiceHost");
        }

        private async Task<bool> NxCloudGetBalance(SMSBlanceCheckOption smsOption)
        {
            try
            {
                var resourceUrl = smsOption.URL;
                var data = new Dictionary<string, string>()
                {
                    {"appkey", smsOption.KEY1},
                    {"secretkey", smsOption.KEY2},
                };

                using var httpResponse = await _httpHelper.HttpFromPostAsync(resourceUrl, null, data, "application/x-www-form-urlencoded");
                if (httpResponse.StatusCode != HttpStatusCode.OK)
                {
                    _logger.LogError($"httpResponse error URL : {resourceUrl} data:{data} StatusCode: {httpResponse.StatusCode}");
                    return false;
                }

                var content = await httpResponse.Content.ReadAsStringAsync();
                var responseJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                if (!responseJson.TryGetValue("code", out var code))
                {
                    _logger.LogError($"httpResponse error URL : {resourceUrl} content : {content}");
                    return false;
                }

                responseJson.TryGetValue("balance", out var balance);
                if (!code.Equals("0"))
                {
                    _logger.LogError($"NxCloudGetBalance Result error URL : {resourceUrl} responseJson: {responseJson} content : {content}");
                    return false;
                }

                _logger.LogError($"TPName : {smsOption.SMSTPName} AccountName : {smsOption.Accountname} balance : {balance} MinimumLimit : {smsOption.Minimumlimit}");

                if ((int)Convert.ToDecimal(balance) < smsOption.Minimumlimit) await SandLesTalkMessage(smsOption, balance);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            return false;

        }

        private async Task<bool> SkyLineGetBalance(SMSBlanceCheckOption smsOption)
        {
            var account = smsOption.KEY1;
            var passWord = smsOption.KEY2;
            var urlGetBalance = smsOption.URL;
            var dateTime = DateTime.UtcNow.AddHours(8).ToString("yyyyMMddHHmmss");
            var sign = MakeSignString(account, passWord, dateTime);
            var url = $"{urlGetBalance}?account={account}&sign={sign}&datetime={dateTime}";

            try
            {
                using var result = await _httpHelper.HttpPost1Async(url, null, "", "application/json");
                if (result.StatusCode != HttpStatusCode.OK)
                {
                    _logger.LogError($"httpResponse error URL : {url}  StatusCode: {result.StatusCode}");
                    return false;
                }

                var contentResult = await result.Content.ReadAsStringAsync();
                var resultResp = JsonConvert.DeserializeObject<SKYLineSMSResult>(contentResult);
                if (resultResp.status != 0)
                {
                    _logger.LogError($"httpResponse error URL : {url}  resultResp: {contentResult}");
                    return false;
                }

                _logger.LogError($"TPName : {smsOption.SMSTPName} AccountName : {smsOption.Accountname} balance : {resultResp.balance} MinimumLimit : {smsOption.Minimumlimit}");
                if ((int)Convert.ToDecimal(resultResp.balance) < smsOption.Minimumlimit) await SandLesTalkMessage(smsOption, resultResp.balance);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            return false;
        }

        private async Task<bool> SandLesTalkMessage(SMSBlanceCheckOption smsOption, object? balance)
        {
            try
            {
                var postData = new
                {
                    GroupName = _letsTalkGroupName,
                    Message = MakeAlertMessage(smsOption, balance?.ToString())
                };

                var parameters = JsonConvert.SerializeObject(postData);

                var httpResponse = await _httpHelper.HttpPost1Async($"{_commonServiceHost}/Common/LetsTalk/SendMessage",
                    null, parameters, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return false;
            }
            return true;
        }

        private static string MakeAlertMessage(SMSBlanceCheckOption smsOption, string balance)
        {
            var message =
                    $"* SMS Balance Alert * \r\n" +
                    $"* Time : {DateTime.UtcNow.AddHours(8):yyyy-MM-dd HH:mm:ss} * \r\n" +
                    $"=======================\r\n" +
                    $"The balance of SMS is less than {smsOption.Minimumlimit}.\r\n" +
                    $"Please add value.\r\n" +
                    $"=======================\r\n" +
                    $"TPName : {smsOption.SMSTPName}\r\n" +
                    $"AccountName : {smsOption.Accountname}\r\n" +
                    $"Now Balance : {balance}\r\n"
                ;
            return message;
        }

        private string MakeSignString(string account, string pwd, string datetime)
        {
            var tmpStr = $"{account}{pwd}{datetime}";
            return _secureHelper.ToMD5(tmpStr);

        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");

            foreach (var item in _smsConfig)
            {
                if (item.Value.SMSTPName.Equals("SkyLine"))
                {
                    await SkyLineGetBalance(item.Value);
                }
                else
                {
                    await NxCloudGetBalance(item.Value);
                }
            }

            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} End......");

            return "true";

        }
    }
}
