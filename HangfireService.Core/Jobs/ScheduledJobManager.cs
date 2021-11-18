using Hangfire;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HangfireService.Core.Jobs
{
    public static class ScheduledJobManager
    {
        public static void Setup(IServiceProvider provider)
        {
            var jobGroups = provider.GetRequiredService<Dictionary<string, List<JobOption>>>();
            var jobManager = new RecurringJobManager();
            foreach (var jobGroup in jobGroups)
            {          
                foreach (var option in jobGroup.Value)
                { 
                    if (option.Enable && option.Type == typeof(IScheduledJob).Name)
                    {
                        var job = provider.GetServices<IScheduledJob>()
                            .First(job => job.GetType().Name.Equals(option.ClassName, StringComparison.OrdinalIgnoreCase));
                    
                        jobManager.AddOrUpdate(
                          recurringJobId: option.JobName,
                          methodCall: () => job.ExecuteAsync(option.JobName, option, null,null),
                          cronExpression: option.Cron,
                          queue: jobGroup.Key.ToLower(),
                          timeZone:TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time"));
                    }
                }
            }
        }

    }
}
