using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Newtonsoft.Json;
using System.ComponentModel;
using Hangfire.Server;
using Hangfire;
using Hangfire.Dashboard.Management.Metadata;
using HangfireService.Core.Models.Options;
using Hangfire.Dashboard.Management.Support;
using HangfireService.Core.Extensions;
using HangfireService.Core.Helpers;
using HangfireService.Core.Models.Enums;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("LeaderboardJob", queue: "Report")]
    public class LeaderboardJob : BaseJob, IScheduledJob
    {
        private readonly ReportDBDataLayer _reportDBDataLayer;
        private readonly ICacheHelper _redisHelper;
        private static List<JobOption> _jobList;

        public LeaderboardJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
            MyJobManager.GetJobGroups()?.TryGetValue("Report", out _jobList);
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");
                var option = JsonConvert.DeserializeObject<LeaderboardOption>(jobOption.Parameters);
                var result = new Dictionary<int, int>();
                var eventCode = option.EventCode;
                var siteId = option.SiteId;
                foreach (var beforeDay in option.beforeDays)
                {               
                    foreach (var categoryId in option.CategoryList)
                    {
                        foreach (var currencyId in option.CurrencyList)
                        {
                            var(startTime, endTime) = GetTimeRange(option, beforeDay);
                            //Get Data
                            var leaderboardList = _reportDBDataLayer.GetLeaderboardList(categoryId, eventCode, siteId, currencyId, option.TopNum, startTime, endTime);

                            //根據ExecuteType設定Redis Key
                            var key = option.ExecuteType switch
                            {
                                ExecuteType.Months => $"LeaderboardListMonth:{categoryId}:{eventCode}:{siteId}:{currencyId}:{startTime:yyyyMM}",
                                ExecuteType.Days => $"LeaderboardListDays:{categoryId}:{eventCode}:{siteId}:{currencyId}:{beforeDay}",
                                _ => throw new NotImplementedException()
                            };
                            _redisHelper.StringSet(key, JsonConvert.SerializeObject(leaderboardList), null);
                            result.Add(currencyId, leaderboardList.Count());
                        }
                    }
                }

                return JsonConvert.SerializeObject(new
                {
                    Success = result,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return JsonConvert.SerializeObject(ex);
            }

        }

        private (DateTime StartTime, DateTime EndTime) GetTimeRange(LeaderboardOption option, int beforeDay)
        {
            var now = DateTime.UtcNow;
            var startTime = option.StartTime ?? new DateTime(now.Year, now.Month, 1);
            var endTime = option.EndTime ?? startTime.GetMonthEnd();
            
            if (option.ExecuteType == ExecuteType.Days)
            {
                startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);  // 當月份1號開始
                endTime = DateTime.UtcNow.Date.AddDays(beforeDay * -1).AddDays(1).AddSeconds(-1);

                if (startTime > endTime)
                {
                    (startTime, endTime) = (endTime, startTime);
                    startTime = startTime.Date;
                }
            }

            return (startTime, endTime);
        }

        [Job]
        [DisplayName("Run {2} by ManagementPage")]
        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public object Run(PerformContext context = null, IJobCancellationToken cancellationToken = null,
        [DisplayData("JobName", ConvertType = typeof(JobList))] string jobName = null,
        [DisplayData("Month", placeholderText: "yyyy-MM", defaultValue: "2023-04")] string redoMonth = null)
        {
            if (!_jobList.Any())
            {
                return null;
            }
            var jobOption = _jobList.First(i => i.JobName == jobName);
            var start = DateTime.ParseExact($"{redoMonth}", "yyyy-MM", null);


            var StartTime = start.ToString("yyyy-MM-dd HH:mm:ss");
            var EndTime = start.GetMonthEnd().ToString("yyyy-MM-dd HH:mm:ss");
            jobOption.Parameters = jobOption.Parameters.Update("StartTime", StartTime);
            jobOption.Parameters = jobOption.Parameters.Update("EndTime", EndTime);
            var result = ExecuteAsync($"Execute.{jobName}.({StartTime} ~ {EndTime})", jobOption, context, cancellationToken).Result;

            return result;
        }

        private class JobList : IInputDataList
        {
            public Dictionary<string, string> GetData()
            {
                return _jobList?.Where(i => i.JobName.Contains("LeaderBoard.Nova88.Months")).ToDictionary(f => f.JobName, f => f.JobName);
            }
            public string GetDefaultValue()
            {
                return null;
            }
        }
    }
}
