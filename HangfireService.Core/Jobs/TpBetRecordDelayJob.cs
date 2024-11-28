using Hangfire;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using Hangfire.Server;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Helpers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System.ComponentModel;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("TpBetRecordDelayJob", queue: "check")]
    internal class TpBetRecordDelayJob : BaseJob, IScheduledJob
    {
        private readonly ReportDBDataLayer _reportDBDataLayer;
        private readonly TelegramBotHelper _telegramBotHelper;
        private static List<JobOption> _jobList;
        private readonly int _delay = 120;

        public TpBetRecordDelayJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
            _telegramBotHelper = provider.GetRequiredService<TelegramBotHelper>();
            MyJobManager.GetJobGroups()?.TryGetValue("Check", out _jobList);
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");
                // 暫時用來檢查SABA未結帳注單
                var checkTpIdList = new List<int> { 41 };
                var cnt = 0;
                var msg = string.Empty;
                var msgRow = string.Empty;

                foreach (var tpId in checkTpIdList)
                {
                    var data = await _reportDBDataLayer.GetTpBetRecordDelayByTpId(tpId);
                    if (data.Any())
                    {
                        msgRow = $"\r\nTpId: {tpId}";
                        foreach (var item in data)
                        {
                            if (item.TimeSecDiff >= _delay)
                            {
                                msgRow += $"\r\nSQ: {item.SQ}, TpGameType: {item.GameType}, DelayTime: {item.TimeSecDiff}";
                                cnt++;
                            }
                        }
                    }
                    //else
                    //{
                    //    _logger.LogError($"Error in retrieving inspection data: {tpId}");
                    //    throw new InvalidOperationException($"Error in retrieving inspection data: {tpId}");
                    //}
                }

                msg = $"[Checkout delay check: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Delay count: {cnt}";

                if (cnt > 0)
                {
                    _telegramBotHelper.SendMessage(msg);
                    //_logger.LogError($"{msg}\r\n{msgRow}");
                }
                else
                {
                    _logger.LogInformation($"{msg}");
                }
                return msg;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return JsonConvert.SerializeObject(ex);
            }

        }

        private class TpBetRecordDelayJobList : IInputDataList
        {
            public Dictionary<string, string> GetData()
            {
                return _jobList?.Where(i => i.JobName.Contains("TpBetRecordDelay.Check"))
                    .ToDictionary(f => f.JobName, f => f.JobName);
            }

            public string GetDefaultValue()
            {
                return null;
            }
        }

        [Job]
        [DisplayName("Run {2} by ManagementPage")]
        public object Run(
            PerformContext context = null,
            IJobCancellationToken cancellationToken = null,
            [DisplayData("JobName", ConvertType = typeof(TpBetRecordDelayJobList))]
            string jobName = null)
        {
            if (!_jobList.Any())
            {
                return null;
            }

            var jobOption = _jobList.First(i => i.JobName == jobName);

            return ExecuteAsync($"Execute.{jobName}", jobOption, context, cancellationToken).Result;
        }
    }
}
