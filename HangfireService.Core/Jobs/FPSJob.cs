using System.ComponentModel;
using Hangfire;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using Hangfire.Server;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Extensions;
using HangfireService.Core.Helpers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Service;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("FPSdJob", queue: "Check")]
    public class FPSJob : BaseJob, IScheduledJob
    {
        private readonly ReportDBDataLayer _reportDBDataLayer;
        private readonly ICacheHelper _redisHelper;
        private readonly FPSService _fpsService;
        private static List<JobOption> _jobList;
        
        public FPSJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
            _fpsService = provider.GetRequiredService<FPSService>();
            MyJobManager.GetJobGroups()?.TryGetValue("Check", out _jobList);
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            var option = JsonConvert.DeserializeObject<FPSOption>(jobOption.Parameters);
            
            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");
            try
            {
                await _fpsService.GetFPSDataAsyncJob(option.SiteId,option.Subscribers,option.Token,option.ApiUrl,option.Mode);
                await _fpsService.GetFPSRiskLevelDataAsyncJob(option.SiteId,option.Subscribers,option.Token,option.ApiUrl,option.Mode);
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
            // jobOption.Parameters = jobOption.Parameters.Update("StartTime", StartTime);
            // jobOption.Parameters = jobOption.Parameters.Update("EndTime", EndTime);

            var result = ExecuteAsync($"Execute.{jobName}", jobOption, context, cancellationToken).Result;


            return result;
        }

        private class JobList : IInputDataList
        {
            public Dictionary<string, string> GetData()
            {
                return _jobList?.Where(i => i.JobName.Contains("FPSJob"))
                    .ToDictionary(f => f.JobName, f => f.JobName);
            }
            public string GetDefaultValue()
            {
                return null;
            }
        }
    }
}