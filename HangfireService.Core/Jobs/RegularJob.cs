using System.ComponentModel;
using Hangfire;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using Hangfire.Server;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Service;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace HangfireService.Core.Jobs
{
    public class RegularJob : BaseJob, IScheduledJob
    {
        private static List<JobOption> _jobList;
        private readonly RegularService _regularService;
        public RegularJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _regularService = provider.GetRequiredService<RegularService>();
            MyJobManager.GetJobGroups()?.TryGetValue("Check", out _jobList);
        }

        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");
            try
            {
                //DB定期排程Job
                //統一傳JobOption.Parameters到Regular裡面再自己解析要的Model，若不需要不傳也沒事。
                await _regularService.SetCustFPSTimeUpdate();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message, $"{DateTime.Now:HH:mm:ss} || {jobName} exception......");
                return false;
            }
            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} End......");
            return true;
        }
        
        [Job]
        [DisplayName("Run {2} by ManagementPage")]
        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public object Run(PerformContext context = null, IJobCancellationToken cancellationToken = null, 
            [DisplayData("JobName", ConvertType = typeof(JobList))] string jobName = null)
        {
            if (!_jobList.Any())
            {
                return null;
            }
            var jobOption = _jobList.First(i => i.JobName == jobName);

            var result = ExecuteAsync($"Execute.{jobName}", jobOption, context, cancellationToken).Result;


            return result;
        }

        private class JobList : IInputDataList
        {
            public Dictionary<string, string> GetData()
            {
                return _jobList?.Where(i => i.JobName.Contains("RegularJob"))
                    .ToDictionary(f => f.JobName, f => f.JobName);
            }
            public string GetDefaultValue()
            {
                return null;
            }
        }
    }
}