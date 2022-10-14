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

namespace HangfireService.Core.Jobs
{
    [ManagementPage("RelayJob", queue: "relay")]
    public class RelayJob : BaseJob, IScheduledJob
    {
        private readonly MainDBDataLayer _mainDBDataLayer;
        private static List<JobOption> _jobList;
        private readonly TelegramBotHelper _telegramBotHelper;

        public RelayJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            MyJobManager.GetJobGroups()?.TryGetValue("Relay", out _jobList);
            _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
            _telegramBotHelper = provider.GetRequiredService<TelegramBotHelper>();
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");
                var option = JsonConvert.DeserializeObject<RelayOption>(JsonConvert.SerializeObject(jobOption.Parameters));
                var result = 0;

                if (option.ExecuteType == ExecuteType.Days)
                {
                    var date = option.StartTime;
                    while (date <= option.EndTime)
                    {
                        var parameters = new
                        {
                            StartTime = date,
                            EndTime = date.AddDays(1),
                        };
                        result += _mainDBDataLayer.ExcuteRelayData(option.ExecuteSP, parameters, option.TimeoutSeconds);
                        date = date.AddDays(1);
                    }
                }
                else
                {
                    result = _mainDBDataLayer.ExcuteRelayData(option.ExecuteSP, option, option.TimeoutSeconds);
                }
                
                return $"{jobName}.({option.StartTime:yyyy-MM-dd HH:mm} ~ {option.EndTime:yyyy-MM-dd HH:mm}) success count: {result}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                BackgroundJob.Schedule(
                    () => ExecuteAsync(jobName, jobOption, null, null),
                    TimeSpan.FromMinutes(5));
                _telegramBotHelper.SendMessage($"{jobName} 執行發生錯誤，五分鐘後重試。");
                return JsonConvert.SerializeObject(ex);
            }
        }

        [Job]
        [DisplayName("Run {2} by ManagementPage")]
        public object Run(
            PerformContext context = null,
            IJobCancellationToken cancellationToken = null,
            [DisplayData("JobName", ConvertType = typeof(JobList))] string jobName = null,
            [DisplayData("RedoBeginDate", ConvertType = typeof(RedoBeginDate))] string redoBeginDate = null)
        {
            if (!_jobList.Any())
            {
                return null;
            }
            var jobOption = _jobList.First(i => i.JobName == jobName);
            var date = DateTime.ParseExact($"{redoBeginDate}", "yyyy-MM-dd", null);
            var StartTime = date.ToString("yyyy-MM-dd");
            var EndTime = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            jobOption.Parameters.Update("StartTime", StartTime);
            jobOption.Parameters.Update("EndDate", EndTime);

            return ExecuteAsync($"Execute.{jobName}.({StartTime} ~ {EndTime})", jobOption, context, cancellationToken).Result;
        }

        private class JobList : IInputDataList
        {
            public Dictionary<string, string> GetData()
            {
                return _jobList?.Where(i => i.JobName.Contains("Relay-LastDays"))
                    .ToDictionary(f => f.JobName, f => f.JobName);
            }
            public string GetDefaultValue()
            {
                return null;
            }
        }

        private class RedoBeginDate : IInputDataList
        {
            public Dictionary<string, string> GetData()
            {
                var yesterday = DateTime.UtcNow.Date.AddDays(-1);
                var begin = yesterday.AddDays(-10);
                var result = new List<string>();
                while (begin <= yesterday)
                {
                    result.Add(begin.ToString("yyyy-MM-dd"));
                    begin = begin.AddDays(1);
                }
                return result.OrderByDescending(i => i).ToDictionary(f => f, f => f);
            }

            public string GetDefaultValue()
            {
                return null;
            }
        }
    }
}
