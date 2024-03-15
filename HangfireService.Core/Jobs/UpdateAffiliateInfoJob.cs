using System.ComponentModel;
using Hangfire;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using Hangfire.Server;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Affiliate;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Service;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using static System.Int32;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("UpdateAffiliateInfoJob", queue: "member")]
    public class UpdateAffiliateInfoJob : BaseJob, IScheduledJob
    {
        private readonly UpdateAffiliateInfoService _updateAffiliateInfoService;
        private static List<JobOption> _jobList;

        public UpdateAffiliateInfoJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _updateAffiliateInfoService = provider.GetRequiredService<UpdateAffiliateInfoService>();
            MyJobManager.GetJobGroups()?.TryGetValue("Member", out _jobList);
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context,
            IJobCancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");

            var siteList = JsonConvert.DeserializeObject<AffiliatePara>(jobOption.Parameters).SiteList;

            foreach (var siteModel in siteList)
            {
                await _updateAffiliateInfoService.RunJobAsync(siteModel.SiteId , siteModel.SiteShortName);
            }
            
            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} End......");

            return $"Success";
        }

        [Job]
        [DisplayName("reUpdateAffiliateInfoJob")]
        public object Run(PerformContext? context = null, IJobCancellationToken? cancellationToken = null, 
            [DisplayData("JobName", ConvertType = typeof(JobList))] string jobName = null,
            [DisplayData("SiteId", defaultValue: "1")] string redoSiteId = null,
            [DisplayData("SiteShortName", defaultValue: "N88")] string redoSiteShortName = null)
        {
            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || reUpdateAffiliateInfoJob running......");

            var result = 0;
            var tryParse = TryParse(redoSiteId , out var siteId);
            if (tryParse)
            {
                result = _updateAffiliateInfoService.RunJobAsync(siteId, redoSiteShortName).Result;
            }
            
            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || reUpdateAffiliateInfoJob End......");

            return result;
        }
        
        private class JobList : IInputDataList
        {
            public Dictionary<string, string> GetData()
            {
                return _jobList?
                    .Where(i => i.JobName.Contains("UpdateAffiliateInfoJob"))
                    .ToDictionary(f => f.JobName, f => f.JobName);
            }
            public string GetDefaultValue()
            {
                return null;
            }
        }
    }
}
