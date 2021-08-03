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

        public Task Execute(JobDetail jobDetail)
        {
            var jobGuid = Guid.NewGuid();
            LogContext.PushProperty("JobGuid", jobGuid);
            LogContext.PushProperty("JobName", jobDetail.JobName);
            try
            {
                _logger.LogInformation($"{jobGuid} || {DateTime.Now:HH:mm:ss} || {jobDetail.JobName} running......");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return Task.CompletedTask;
        }

        [Job]
        [DisplayName("Call LiveJob")]
        public void Action(PerformContext context = null, IJobCancellationToken cancellationToken = null)
        {
            _ = Execute(new JobDetail() { JobName = "LiveJob" });
        }
    }
}
