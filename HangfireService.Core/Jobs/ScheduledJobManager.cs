using Hangfire;
using Hangfire.Common;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
                    recurringJobId: job.JobDetailOption.JobName,
                    methodCall: () => job.Execute(),
                    cronExpression: job.JobDetailOption.Cron);
            }            
        }

    }
}
