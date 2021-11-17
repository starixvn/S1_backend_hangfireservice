using Hangfire;
using Hangfire.Server;
using HangfireService.Core.Models.Jobs;

namespace HangfireService.Core.Jobs.Interfaces
{
    public interface IScheduledJob
    {
        Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken);
    }
}
