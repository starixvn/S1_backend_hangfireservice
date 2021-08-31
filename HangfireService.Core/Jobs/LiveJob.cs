using Hangfire;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using Hangfire.Server;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("Test")]
    public class LiveJob : BaseJob, IScheduledJob
    {
        public LiveJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobDetail jobDetail)
        {
            var result = $"{DateTime.Now:HH:mm:ss} || {jobName} running......";
            _logger.LogInformation(result);
            return result;
        }

        [Job]
        [DisplayName("Call LiveJob")]
        public void Action(PerformContext context = null, IJobCancellationToken cancellationToken = null)
        {
            _ = ExecuteAsync("Call LiveJob", new JobDetail() { JobName = "LiveJob" });
        }
    }
}
