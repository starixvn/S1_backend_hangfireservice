using Hangfire;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HangfireService.Core.Jobs
{
    public class ScheduledJobManager
    {
        public static void Setup(IServiceProvider provider, IConfiguration configuration)
        {
            var jobGroups = new Dictionary<string, List<JobOption>>();
            configuration.GetSection("ScheduledJob").Bind(jobGroups);

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
                          methodCall: () => job.ExecuteAsync(option.JobName, option),
                          cronExpression: option.Cron,
                          queue: jobGroup.Key.ToLower());
                    }
                }
            }
        }

    }
}
