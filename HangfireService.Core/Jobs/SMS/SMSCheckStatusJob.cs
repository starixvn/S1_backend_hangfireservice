using System.ComponentModel;
using System.Net;

using Amazon.CloudWatchLogs.Model;

using Hangfire;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using Hangfire.Server;

using HangfireService.Core.DataLayers;
using HangfireService.Core.Helpers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Jobs;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Serilog.Context;

namespace HangfireService.Core.Jobs.SMS
{
    [ManagementPage("SMSCheckStatus", queue: "check")]
    public class SMSCheckStatusJob : BaseJob, IScheduledJob
    {
        private readonly HttpHelper _httpHelper;
        private readonly SecureHelper _secureHelper;
        private readonly AWSCloudWatchLogsHelper _awsSnsHelper;

        private readonly ReportDBDataLayer _reportDbDataLayer;
        private readonly MainDBDataLayer _mainDbDataLayer;
        private readonly Dictionary<string, Func<OTPCheck, Task<OTPCheckResult>>> _providerHandler;
        private readonly Func<OTPCheck, Task<OTPCheckResult>> _defaultCheck;

        private readonly Dictionary<int, NxCloudSmsConfig> _nxCloudSmsConfig;
        private readonly Dictionary<int, SkyLineSmsConfig> _skyLineSmsConfig;
        private readonly Dictionary<int, SkyLineOtpConfig> _skyLineOtpConfig;
        private readonly Dictionary<int, AwsSmsConfig> _awsSmsConfig;

        private static List<JobOption> _jobList;

        public SMSCheckStatusJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _secureHelper = provider.GetRequiredService<SecureHelper>();
            _awsSnsHelper = provider.GetRequiredService<AWSCloudWatchLogsHelper>();

            var configBll = provider.GetRequiredService<ConfigBLL>();
            _nxCloudSmsConfig = configBll.GetConfigValuesBySiteId<NxCloudSmsConfig>("HangfireNxCloud");
            _skyLineSmsConfig = configBll.GetConfigValuesBySiteId<SkyLineSmsConfig>("HangfireSkyLineSMS");
            _skyLineOtpConfig = configBll.GetConfigValuesBySiteId<SkyLineOtpConfig>("HangfireSkyLineOTP");
            _awsSmsConfig = configBll.GetConfigValuesBySiteId<AwsSmsConfig>("HangfireAwsSms");

            MyJobManager.GetJobGroups()?.TryGetValue("Check", out _jobList);

            _reportDbDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
            _mainDbDataLayer = provider.GetRequiredService<MainDBDataLayer>();
            _providerHandler = new Dictionary<string, Func<OTPCheck, Task<OTPCheckResult>>>()
            {
                { "SkyLine", SkyLineCheckStatus},
                { "NxCloud", NxCloudCheckStatus},
                { "AWS", AwsCheckStatus}
            };
            _defaultCheck = CheckStatusByPass;
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            try
            {
                var checkList = _reportDbDataLayer.GetOTPCheckList();
                foreach (var otp in checkList)
                {

                    var result = _providerHandler.TryGetValue(otp.OTPProvider, out var func)
                        ? await func.Invoke(otp)
                        : await _defaultCheck.Invoke(otp);

                    if (result.OTPStatus is (int)OTPStatus.SendToCustomer or (int)OTPStatus.Fail)
                    {
                        _mainDbDataLayer.SetOTPStatus(result);
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

        private async Task<OTPCheckResult> SkyLineCheckStatus(OTPCheck otpCheck)
        {
            try
            {
                // SkyLine發送SMS走舊的domain發送，舊的domain無法正常檢查，先改成Pass
                // TODO: S1 - Currently it can check status well, comment this below lines.
                //if (string.IsNullOrWhiteSpace(otpCheck.OTPNum))
                //{
                //    return otpCheck.GetResult(OTPStatus.SendToCustomer);
                //}
                if (string.IsNullOrWhiteSpace(otpCheck.Request_id))
                {
                    return otpCheck.GetResult(OTPStatus.SendToCustomer);
                }

                var smsOption = GetSkyLineOption(otpCheck);
                var timestamp = new DateTimeOffset(DateTime.Now).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
                var sign = _secureHelper.ToMD5($"{smsOption.ApiKey}{smsOption.ApiSecret}{timestamp}");
                var url = $"{smsOption.CheckUrl}?appId={smsOption.Key1}&msgIds={otpCheck.Request_id}";

                var headerParams = new Dictionary<string, string>
                {
                    { "Sign", sign },
                    { "Timestamp", timestamp.ToString() },
                    { "Api-Key", smsOption.ApiKey },
                };

                using var result = await _httpHelper.HttpGetAsync(url, headerParams, "application/json");
                if (result.StatusCode != HttpStatusCode.OK)
                {
                    _logger.LogError($"SkyLine httpResponse error URL : {url}  StatusCode: {result.StatusCode}");
                    return otpCheck.GetResult(OTPStatus.SendToProvider);
                }

                var contentResult = await result.Content.ReadAsStringAsync();
                var resultResp = JsonConvert.DeserializeObject<SKYLineOTPResult>(contentResult);

                if (resultResp.status != 0)
                {
                    _logger.LogError($"SkyLine status error URL : {url}  resultResp: {contentResult}");
                    return otpCheck.GetResult(OTPStatus.SendToProvider);
                }

                var smsResult = resultResp.array.FirstOrDefault(x => x.msgId == otpCheck.Request_id);
                if (smsResult == null) //回傳的array找不到同一個Request_id
                {
                    return otpCheck.GetResult(OTPStatus.Fail, $" || SkyLine no found Request_id:{otpCheck.Request_id}.");
                }

                var status = smsResult.status switch
                {
                    "0" => OTPStatus.SendToCustomer, //发送成功              
                    "1" => OTPStatus.Fail, //未发送
                    "-1" => OTPStatus.SendToProvider, //正在发送
                    _ => OTPStatus.Fail,
                };

                return otpCheck.GetResult(status, $"check status is {smsResult.status}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return otpCheck.GetResult(OTPStatus.SendToProvider);
            }
        }

        private async Task<OTPCheckResult> NxCloudCheckStatus(OTPCheck otpCheck)
        {
            try
            {
                LogContext.PushProperty("TP", "NxCloud");
                LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

                var nxCloudConfig = GetNxCloudOption(otpCheck);
                var url = nxCloudConfig.CheckUrl;
                var data = new Dictionary<string, string>()
                {
                    { "appkey", nxCloudConfig.Key1},
                    { "secretkey", nxCloudConfig.Key2},
                    { "date", otpCheck.CreateTime.AddHours(8).ToString("yyyyMMdd")},
                    { "messageid", otpCheck.Request_id}
                };

                LogContext.PushProperty("TpInfo", JsonConvert.SerializeObject(data));

                using var httpResponse = await _httpHelper.HttpFromPostAsync(url, null, data, "application/x-www-form-urlencoded");
                if (httpResponse.StatusCode != HttpStatusCode.OK)
                {
                    _logger.LogWarning($"httpResponse error URL : {url} data:{data} StatusCode: {httpResponse.StatusCode}");
                    return otpCheck.GetResult(OTPStatus.SendToProvider);
                }

                var content = await httpResponse.Content.ReadAsStringAsync();
                _logger.LogWarning($"httpResponse Result URL : {url} content : {content} StatusCode: {httpResponse.StatusCode}");
                var result = JsonConvert.DeserializeObject<NxCloudCheckResult>(content);

                if (result.code == "3")
                {
                    return otpCheck.GetResult(OTPStatus.Fail);
                }

                if (result.code != "0")
                {
                    return otpCheck.GetResult(OTPStatus.SendToProvider);
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
                    _ => OTPStatus.SendToProvider
                };

                return otpCheck.GetResult(status, $"check status is {result.row.dr}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return otpCheck.GetResult(OTPStatus.SendToProvider);
            }
        }

        private Task<OTPCheckResult> AwsCheckStatus(OTPCheck otpCheck)
        {
            try
            {
                var customer = _reportDbDataLayer.GetCustInfo(otpCheck.CustId);
                var awsSmsConfig = _awsSmsConfig[customer.SiteId];
                var startQueryRequest = new StartQueryRequest
                {
                    LogGroupNames = new List<string>
                    {
                        awsSmsConfig.Key1,
                        awsSmsConfig.Key2
                    },
                    StartTime = otpCheck.CreateTimeToUnix - 120,
                    EndTime = ((DateTimeOffset)DateTime.UtcNow.AddSeconds(-1)).ToUnixTimeSeconds(),
                    QueryString = "fields @timestamp, @message" +
                                  $" | filter @message like /{otpCheck.Request_id}/" +
                                  " | sort @timestamp",
                    Limit = 100
                };
                var queryId = _awsSnsHelper.GetCloudWatchLogsQueryId(startQueryRequest);

                BackgroundJob.Schedule(
                    () => AwsOtpResult(otpCheck, queryId, true, JsonConvert.SerializeObject(startQueryRequest)),
                    TimeSpan.FromMinutes(1));

                return Task.FromResult(otpCheck.GetResult(OTPStatus.SendToProvider));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Task.FromResult(otpCheck.GetResult(OTPStatus.SendToProvider));
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

        private Task<OTPCheckResult> CheckStatusByPass(OTPCheck otpCheck)
        {
            return Task.FromResult(otpCheck.GetResult(OTPStatus.SendToCustomer));
        }

        public Task<OTPCheckResult> AwsOtpResult(OTPCheck otpCheck, string queryId, bool retry,
            string queryString)
        {
            LogContext.PushProperty("TP", "AWSCloudWatchLogs");
            LogContext.PushProperty("RequestId", queryId);
            LogContext.PushProperty("TpInfo", JsonConvert.SerializeObject(queryString));

            try
            {
                var results = _awsSnsHelper.GetCloudWatchLogsQueryResults(queryId);

                var status = OTPStatus.SendToProvider;
                var message = "";
                if (results.Any())
                {
                    var awsResult = results.FirstOrDefault(i => i.notification.messageId == otpCheck.Request_id);
                    status = awsResult?.status switch
                    {
                        "SUCCESS" => OTPStatus.SendToCustomer, // 成功
                        "FAILURE" => OTPStatus.Fail, // 失败
                        _ => OTPStatus.SendToProvider
                    };
                    message = awsResult?.delivery.providerResponse;
                }

                var result = otpCheck.GetResult(status, message);
                if (result.OTPStatus is (int)OTPStatus.SendToCustomer or (int)OTPStatus.Fail)
                {
                    _mainDbDataLayer.SetOTPStatus(result);
                    return Task.FromResult(result);
                }

                if (retry)
                {
                    //再次嘗試跟 AWS 要 OTP 發送狀態，確保 AWS 已處理完請求
                    BackgroundJob.Schedule(
                        () => AwsOtpResult(otpCheck, queryId, false, queryString),
                        TimeSpan.FromMinutes(3));
                }
                else
                {
                    //嘗試一次後拿不到資料直接更新資料，避免無窮迴圈
                    _mainDbDataLayer.SetOTPStatus(otpCheck.GetResult(OTPStatus.SendToCustomer, "系統檢查2次自動Pass"));
                }

                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Task.FromResult(otpCheck.GetResult(OTPStatus.SendToProvider));
            }
        }

        private SmsWithOtpBaseConfig GetSkyLineOption(OTPCheck otpCheck)
        {
            var customer = _reportDbDataLayer.GetCustInfo(otpCheck.CustId);

            //return string.IsNullOrWhiteSpace(otpCheck.OTPNum)
            //    ? _skyLineSmsConfig[customer.SiteId]
            //    : _skyLineOtpConfig[customer.SiteId];

            //TODO: S1 - Currently, only SkyLineOTPConfig is being used.
            return _skyLineOtpConfig[customer.SiteId];
        }

        private SmsWithOtpBaseConfig GetNxCloudOption(OTPCheck otpCheck)
        {
            var customer = _reportDbDataLayer.GetCustInfo(otpCheck.CustId);
            return _nxCloudSmsConfig[customer.SiteId];
        }
    }
}
