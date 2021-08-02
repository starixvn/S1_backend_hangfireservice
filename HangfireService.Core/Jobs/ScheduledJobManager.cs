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
            var jobOptions = new List<JobDetail>();
            configuration.GetSection("Jobs").Bind(jobOptions);

            var jobManager = new RecurringJobManager();
            foreach (var options in jobOptions)
            {
                if (options.Enable && options.Type == typeof(IScheduledJob).Name)
                {
                    var job = provider.GetServices<IScheduledJob>()
                        .First(job => job.GetType().Name.Equals(options.ClassName, StringComparison.OrdinalIgnoreCase));
                    
                    jobManager.AddOrUpdate(
                      recurringJobId: options.JobName,
                      methodCall: () => job.Execute(options),
                      cronExpression: options.Cron);
                }
            }
        }

    }
}
