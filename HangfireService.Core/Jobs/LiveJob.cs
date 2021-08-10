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

        public async Task<object> ExecuteAsync(JobDetail jobDetail)
        {
            var result = $"{DateTime.Now:HH:mm:ss} || {jobDetail.JobName} running......";
            _logger.LogInformation(result);
            return result;
        }

        [Job]
        [DisplayName("Call LiveJob")]
        public void Action(PerformContext context = null, IJobCancellationToken cancellationToken = null)
        {
            _ = ExecuteAsync(new JobDetail() { JobName = "LiveJob" });
        }
    }
}
