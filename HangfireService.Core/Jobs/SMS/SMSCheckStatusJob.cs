using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Newtonsoft.Json;
using System.ComponentModel;
using Hangfire.Server;
using Hangfire;
using HangfireService.Core.Models.Options;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using HangfireService.Core.Helpers;
using Microsoft.Extensions.Configuration;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using System.Net;
using Amazon.CloudWatchLogs.Model;

namespace HangfireService.Core.Jobs.SMS
{
    [ManagementPage("SMSCheckStatus", queue: "sms")]
    public class SMSCheckStatusJob : BaseJob, IScheduledJob
    {
        protected readonly HttpHelper _httpHelper;
        private readonly SecureHelper _secureHelper;
        private readonly AWSCloudWatchLogsHelper _awsSnsHelper;
        private readonly Dictionary<string, SMSOption> _smsConfig;
        private static List<JobOption> _jobList;
        private readonly ReportDBDataLayer _reportDBDataLayer;
        private readonly MainDBDataLayer _mainDBDataLayer;
        private readonly Dictionary<string, Func<OTPCheck, Task<OTPCheckResult>>> _providerHandler;
        private readonly Func<OTPCheck, Task<OTPCheckResult>> _defaultCheck;

        public SMSCheckStatusJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _secureHelper = provider.GetRequiredService<SecureHelper>();
            _awsSnsHelper = provider.GetRequiredService<AWSCloudWatchLogsHelper>();
            var iConfiguration = provider.GetRequiredService<IConfiguration>();
            _smsConfig = iConfiguration.GetSection("SMSConfig").Get<Dictionary<string, SMSOption>>();
            MyJobManager.GetJobGroups()?.TryGetValue("SMS", out _jobList);
            _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
            _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
            _providerHandler = new Dictionary<string, Func<OTPCheck, Task<OTPCheckResult>>>()
            {
                { "SkyLine", SkyLineCheckStatus},
                { "NxCloud", NxCloudCheckStatus},
                { "AWS", AWSCheckStatus}
            };
            _defaultCheck = CheckStatusByPass;
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            try
            {
                var checkList = _reportDBDataLayer.GetOTPCheckList();
                foreach (var otp in checkList)
                {

                    var result = _providerHandler.TryGetValue(otp.OTPProvider, out var func)
                        ? await func.Invoke(otp)
                        : await _defaultCheck.Invoke(otp);
                    
                    if (result.OTPStatus != (int)OTPStatus.SendToProvider)
                    {
                        _mainDBDataLayer.SetOTPStatus(result);
                    }
                }
                return $"Success check count : {checkList.Count()}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return JsonConvert.SerializeObject(ex);
            }
        }

        [Job]
        [DisplayName("Run {2} by ManagementPage")]
        public object Run(
            PerformContext context = null,
            IJobCancellationToken cancellationToken = null,
            [DisplayData("JobName", ConvertType = typeof(JobList))] string jobName = null)
        {
            if (!_jobList.Any())
            {
                return null;
            }
            var jobOption = _jobList.First(i => i.JobName == jobName);

            return ExecuteAsync($"Execute.{jobName}", jobOption, context, cancellationToken).Result;
        }

        private class JobList : IInputDataList
        {
            public Dictionary<string, string> GetData()
            {
                return _jobList?
                    .Where(i => i.JobName.Contains("SMSCheckStatus"))
                    .ToDictionary(f => f.JobName, f => f.JobName);
            }
            public string GetDefaultValue()
            {
                return null;
            }
        }

        private async Task<OTPCheckResult> SkyLineCheckStatus(OTPCheck oTPCheck)
        {
            try
            {
                var smsOption = GetSkyLineOption(oTPCheck);
                var account = smsOption.KEY1;
                var passWord = smsOption.KEY2;
                var dateTime = DateTime.UtcNow.AddHours(8).ToString("yyyyMMddHHmmss");
                var sign = _secureHelper.ToMD5($"{account}{passWord}{dateTime}");
                var url = $"{smsOption.CheckURL}?account={account}&sign={sign}&datetime={dateTime}&ids={oTPCheck.Request_id}";

                using var result = await _httpHelper.GetClient().GetAsync(url);
                if (result.StatusCode != HttpStatusCode.OK)
                {
                    _logger.LogError($"SkyLine httpResponse error URL : {url}  StatusCode: {result.StatusCode}");
                    return oTPCheck.GetResult(OTPStatus.SendToProvider);
                }

                var contentResult = await result.Content.ReadAsStringAsync();
                var resultResp = JsonConvert.DeserializeObject<SKYLineSMSResult>(contentResult);

                if (resultResp.status != 0)
                {
                    _logger.LogError($"SkyLine status error URL : {url}  resultResp: {contentResult}");
                    return oTPCheck.GetResult(OTPStatus.SendToProvider);
                }

                var smsResult = resultResp.array?.FirstOrDefault();
                if (smsResult == null || smsResult[0] != oTPCheck.Request_id) //回傳的array找不到同一個Request_id
                {
                    return oTPCheck.GetResult(OTPStatus.Fail,$" || SkyLine no found Request_id:{oTPCheck.Request_id}.");
                }

                var status = smsResult[3] switch
                {
                    "0" => OTPStatus.CustomerVerified, //发送成功              
                    "1" => OTPStatus.SendToProvider, //未发送
                    "2" => OTPStatus.SendToProvider, //正在发送
                    _ => OTPStatus.Fail,
                };

                return oTPCheck.GetResult(status, $"check status is {smsResult[3]}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return oTPCheck.GetResult(OTPStatus.SendToProvider);
            }
        }

        private async Task<OTPCheckResult> NxCloudCheckStatus(OTPCheck oTPCheck)
        {

            try
            {
                var smsOption = GetNxCloudOption(oTPCheck);
                var url = smsOption.CheckURL;
                var data = new Dictionary<string, string>()
                {
                    { "appkey", smsOption.KEY1},
                    { "secretkey", smsOption.KEY2},
                    { "date", oTPCheck.CreateTime.AddHours(8).ToString("yyyyMMdd")},
                    { "messageid", oTPCheck.Request_id}
                };

                using var httpResponse = await _httpHelper.HttpFromPostAsync(url, null, data, "application/x-www-form-urlencoded");
                if (httpResponse.StatusCode != HttpStatusCode.OK)
                {
                    _logger.LogError($"httpResponse error URL : {url} data:{data} StatusCode: {httpResponse.StatusCode}");
                    return oTPCheck.GetResult(OTPStatus.SendToProvider);
                }

                var content = await httpResponse.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<NxCloudCheckResult>(content);
                if (result.code != "0")
                {
                    _logger.LogError($"httpResponse Result error URL : {url} content : {content}");
                    return oTPCheck.GetResult(OTPStatus.SendToProvider);
                }

                var status = result.row.dr switch
                {
                    "DELIVRD" => OTPStatus.SendToCustomer, // 成功
                    "UNDELIV" => OTPStatus.Fail, // 失败
                    "REJECTD" => OTPStatus.Fail, // 拒绝
                    "EXPIRED" => OTPStatus.Fail, // 过期
                    "DELETED" => OTPStatus.Fail, // 删除
                    "DND" => OTPStatus.Fail, // 免打扰
                    "ESME_RINVDSTADR" => OTPStatus.Fail, // 错误号码
                    "ESME_RINVSRCADR" => OTPStatus.Fail, // 错误发件人
                    "UNKNOWN" => OTPStatus.SendToProvider, // 未知
                };

                return oTPCheck.GetResult(status, $"check status is {result.row.dr}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return oTPCheck.GetResult(OTPStatus.SendToProvider);
            }
        }

        private async Task<OTPCheckResult> CheckStatusByPass(OTPCheck oTPCheck)
        {
            return oTPCheck.GetResult(OTPStatus.SendToCustomer);
        }
        
        private async Task<OTPCheckResult> AWSCheckStatus(OTPCheck oTPCheck)
        {

            try
            {
                var startQueryRequest = new StartQueryRequest
                {
                    LogGroupName = "sns/ap-southeast-1/761771912479/DirectPublishToPhoneNumber",
                    StartTime = ((DateTimeOffset)oTPCheck.CreateTime.AddHours(8).AddMinutes(-2)).ToUnixTimeSeconds(), 
                    EndTime = ((DateTimeOffset)oTPCheck.CreateTime.AddHours(8).AddMinutes(2)).ToUnixTimeSeconds(),
                    QueryString = "fields @timestamp, @message" +
                                  // $" | filter @message like /{oTPCheck.Request_id}/"+
                                  " | sort @timestamp desc",
                    Limit = 1000
                };
                
                var results = _awsSnsHelper.GetCloudWatchLogs(startQueryRequest);
                if (!results.Any())
                {
                    startQueryRequest.LogGroupName = "sns/ap-southeast-1/761771912479/DirectPublishToPhoneNumber/Failure";
                    results = _awsSnsHelper.GetCloudWatchLogs(startQueryRequest);
                }

                var status = OTPStatus.SendToProvider;
                var message = "";
                if (results.Any())
                {
                    var result = results.FirstOrDefault(i => i.notification.messageId == oTPCheck.Request_id);
                    status = result?.status switch
                    {
                        "SUCCESS" => OTPStatus.SendToCustomer, // 成功
                        "FAILURE" => OTPStatus.Fail, // 失败
                        _ => OTPStatus.SendToProvider
                    };
                    message = result?.delivery.providerResponse;
                }
                
                return oTPCheck.GetResult(status, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return oTPCheck.GetResult(OTPStatus.SendToProvider);
            }
        }

        private SMSOption GetSkyLineOption(OTPCheck oTPCheck)
        {
            return string.IsNullOrWhiteSpace(oTPCheck.OTPNum) ? _smsConfig["SkyLineSMSNova88"] : _smsConfig["SkyLineOTPNova88"];
        }

        private SMSOption GetNxCloudOption(OTPCheck oTPCheck)
        {
            var customer = _reportDBDataLayer.GetCustInfo(oTPCheck.CustId);
            return customer.SiteId switch
            {
                1 => _smsConfig["NXCloudSMSNova88"],
                2 => _smsConfig["NxCloudSMSGoal123"],
                3 => _smsConfig["NxCloudSMSBwing"],
            };
        }
    }
}
