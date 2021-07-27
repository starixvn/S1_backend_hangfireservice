using Hangfire;
using HangfireService.Core.Jobs.Interfaces;
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
                    recurringJobId: job.JobDetailOption.Name, 
                    methodCall:() => job.Execute(),
                    cronExpression: job.JobDetailOption.Cron);
            }            
        }

        public static Type[] GetModuleTypes(IServiceProvider provider)
        {
            var result = new List<Type>();
            var assembly = Assembly.GetExecutingAssembly();
            foreach (var job in provider.GetServices<IScheduledJob>())
            {
                var assemblyType = assembly.GetType($"HangfireService.Core.Jobs.{job.JobDetailOption.Name}");
                result.Add(assemblyType);
            }
            return result.ToArray();
        }
    }
}
