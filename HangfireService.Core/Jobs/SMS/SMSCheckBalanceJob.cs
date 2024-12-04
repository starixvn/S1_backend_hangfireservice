using System.ComponentModel;
using System.Net;

using Hangfire;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using Hangfire.Server;

using HangfireService.Core.Helpers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Models.Options;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Serilog.Context;

namespace HangfireService.Core.Jobs.SMS
{
    [ManagementPage("SMSCheckBalance", queue: "check")]
    public class SMSCheckBalanceJob : BaseJob, IScheduledJob
    {
        protected readonly HttpHelper _httpHelper;
        private readonly SecureHelper _secureHelper;

        private readonly string _letsTalkGroupName;
        private readonly string _commonServiceHost;
        private static List<JobOption> _jobList;

        private readonly IEnumerable<NxCloudSmsConfig> _nxCloudSmsConfig;
        private readonly IEnumerable<SkyLineSmsConfig> _skyLineSmsConfig;
        private readonly IEnumerable<SkyLineOtpConfig> _skyLineOtpConfig;

        public SMSCheckBalanceJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _secureHelper = provider.GetRequiredService<SecureHelper>();

            var configBll = provider.GetRequiredService<ConfigBLL>();
            _nxCloudSmsConfig = configBll.GetRawConfigValues<NxCloudSmsConfig>("HangfireNxCloud");
            _skyLineSmsConfig = configBll.GetRawConfigValues<SkyLineSmsConfig>("HangfireSkyLineSMS");
            _skyLineOtpConfig = configBll.GetRawConfigValues<SkyLineOtpConfig>("HangfireSkyLineOTP");

            // 共用同一組設定
            _letsTalkGroupName = configBll.GetConfigValue<LetsTalkConfig>("HangfireLetsTalk").SnsGroupName;
            _commonServiceHost = configBll.GetServiceHostConfig().CommonService;

            MyJobManager.GetJobGroups()?.TryGetValue("Check", out _jobList);
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");

            var skyLineSmsCheckBalanceTask = SkyLineSmsCheckBalance();
            var skyLineOtpCheckBalanceTask = SkyLineOtpCheckBalance();
            var nxCloudCheckBalanceTask = NxCloudCheckBalance();

            await Task.WhenAll(skyLineSmsCheckBalanceTask,
                skyLineOtpCheckBalanceTask,
                nxCloudCheckBalanceTask);

            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} End......");

            return "true";
        }

        #region CheckBalance

        private async Task NxCloudCheckBalance()
        {
            try
            {
                foreach (var nxCloudConfigItem in _nxCloudSmsConfig)
                {
                    await NxCloudGetBalance(nxCloudConfigItem);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"{DateTime.Now:HH:mm:ss} || nxCloud SMS CheckBalance Job Error Exception:{JsonConvert.SerializeObject(ex)}");
            }
        }

        private async Task SkyLineOtpCheckBalance()
        {
            try
            {
                foreach (var skyLineOtpConfigItem in _skyLineOtpConfig)
                {
                    await SkyLineGetBalance(skyLineOtpConfigItem);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"{DateTime.Now:HH:mm:ss} || skyLine OTP CheckBalance Job Error Exception:{JsonConvert.SerializeObject(ex)}");
            }
        }

        private async Task SkyLineSmsCheckBalance()
        {
            try
            {
                foreach (var skyLineSmsConfigItem in _skyLineSmsConfig)
                {
                    await SkyLineGetBalance(skyLineSmsConfigItem);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"{DateTime.Now:HH:mm:ss} || skyLine Sms CheckBalance Job Error Exception:{JsonConvert.SerializeObject(ex)}");
            }
        }

        #endregion

        private async Task<bool> NxCloudGetBalance(SmsWithOtpBaseConfig nxCloudSmsConfig)
        {
            try
            {
                var resourceUrl = nxCloudSmsConfig.GetBalanceUrl;
                var data = new Dictionary<string, string>()
                {
                    {"appkey", nxCloudSmsConfig.Key1},
                    {"secretkey", nxCloudSmsConfig.Key2},
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

                _logger.LogError($"TPName : {nxCloudSmsConfig.SmsTpName} AccountName : {nxCloudSmsConfig.AccountName} balance : {balance} MinimumLimit : {nxCloudSmsConfig.MiniMumLimit}");

                if ((int)Convert.ToDecimal(balance) < nxCloudSmsConfig.MiniMumLimit)
                {
                    await SandLesTalkMessage(nxCloudSmsConfig, balance);
                    return true;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            return false;

        }

        private async Task<bool> SkyLineGetBalance(SmsWithOtpBaseConfig smsConfig)
        {
            var account = smsConfig.Key1;
            var passWord = smsConfig.Key2;
            var urlGetBalance = smsConfig.GetBalanceUrl;
            var dateTime = DateTime.UtcNow.AddHours(8).ToString("yyyyMMddHHmmss");
            var sign = _secureHelper.ToMD5($"{account}{passWord}{dateTime}");
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

                _logger.LogError($"TPName : {smsConfig.SmsTpName} AccountName : {smsConfig.AccountName} balance : {resultResp.balance} MinimumLimit : {smsConfig.MiniMumLimit}");
                if ((int)Convert.ToDecimal(resultResp.balance) < smsConfig.MiniMumLimit) await SandLesTalkMessage(smsConfig, resultResp.balance);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            return false;
        }

        #region Send Notfication To LestTask

        private async Task<bool> SandLesTalkMessage(SmsWithOtpBaseConfig smsOption, object balance)
        {
            try
            {
                var postData = new
                {
                    GroupName = _letsTalkGroupName,
                    Message = MakeAlertMessage(smsOption, balance?.ToString())
                };

                var parameters = JsonConvert.SerializeObject(postData);

                var httpResponse = await _httpHelper.HttpPost1Async($"{_commonServiceHost}LetsTalk/SendMessage",
                    null, parameters, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return false;
            }
            return true;
        }

        private static string MakeAlertMessage(SmsWithOtpBaseConfig smsOption, string balance)
        {
            var message =
                    $"* SMS Balance Alert * \r\n" +
                    $"* Time : {DateTime.UtcNow.AddHours(8):yyyy-MM-dd HH:mm:ss} * \r\n" +
                    $"=======================\r\n" +
                    $"The balance of SMS is less than {smsOption.MiniMumLimit}.\r\n" +
                    $"Please add value.\r\n" +
                    $"=======================\r\n" +
                    $"TPName : {smsOption.SmsTpName}\r\n" +
                    $"AccountName : {smsOption.AccountName}\r\n" +
                    $"Now Balance : {balance}\r\n"
                ;
            return message;
        }

        #endregion

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
                    .Where(i => i.JobName.Contains("SMSCheckBalance"))
                    .ToDictionary(f => f.JobName, f => f.JobName);
            }
            public string GetDefaultValue()
            {
                return null;
            }
        }
    }
}
