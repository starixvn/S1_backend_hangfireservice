using Hangfire;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using Hangfire.Server;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System.ComponentModel;
using HangfireService.Core.Extensions;
using HangfireService.Core.Models.Options;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("BonusSummaryJob", queue: "relay")]
    public class BonusSummaryJob : BaseJob, IScheduledJob
    {
        private readonly MainDBDataLayer _mainDBDataLayer;
        private readonly ReportDBDataLayer _reportDBDataLayer;
        private static List<JobOption> _jobList;

        public BonusSummaryJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            MyJobManager.GetJobGroups()?.TryGetValue("Relay", out _jobList);
            _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
            _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");
                var option = JsonConvert.DeserializeObject<BonusSummaryOption>(JsonConvert.SerializeObject(jobOption.Parameters));
                switch (option.PromotionType)
                {
                    case PromotionType.Promotion:
                        break;
                    case PromotionType.Rebate:
                        break;
                    case PromotionType.PrizeDraw:
                        break;
                    case PromotionType.VerificationReward:
                        break;
                   default:
                       break;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return JsonConvert.SerializeObject(ex);
            }
        }

        private class JobList : IInputDataList
        {
            public Dictionary<string, string> GetData()
            {
                return _jobList?.Where(i => i.JobName.Contains("BonusSummary"))
                    .ToDictionary(f => f.JobName, f => f.JobName);
            }

            public string GetDefaultValue()
            {
                return null;
            }
        }

        [Job]
        [DisplayName("Run {2} by ManagementPage")]
        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public object Run(
            PerformContext context = null,
            IJobCancellationToken cancellationToken = null,
            [DisplayData("JobName", ConvertType = typeof(JobList))] string jobName = null,
            [DisplayData("BeginDate", placeholderText: "yyyy-MM-dd")] string beginDate = null,
            [DisplayData("EndDate", placeholderText: "yyyy-MM-dd")] string endDate = null)
        {
            if (!_jobList.Any())
            {
                return null;
            }
            var jobOption = _jobList.First(i => i.JobName == jobName);
            var start = DateTime.ParseExact($"{beginDate}", "yyyy-MM-dd", null);
            var end = DateTime.ParseExact($"{endDate}", "yyyy-MM-dd", null);
            
            var startTime = start.ToString("yyyy-MM-dd");
            var endTime = end.ToString("yyyy-MM-dd");
            jobOption.Parameters.Update("StartTime", startTime);
            jobOption.Parameters.Update("EndDate", endTime);
            return ExecuteAsync($"Execute.{jobName}.({startTime} ~ {endTime})", jobOption, context, cancellationToken).Result;
        }
    }
}
