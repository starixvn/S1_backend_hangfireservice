using Hangfire;
using HangfireService.Core.Jobs.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace HangfireService.Core.Jobs
{
    public class ScheduledJobManager
    {
        public static void Setup(IServiceProvider provider)
        {
            var jobManager = new RecurringJobManager();
            foreach (var job in provider.GetServices<IScheduledJob>())
            {
                jobManager.AddOrUpdate(
                    recurringJobId: job.JobDetailOption.Name, 
                    methodCall:() => job.Execute(),
                    cronExpression: job.JobDetailOption.Cron);
            }            
        }
    }
}
