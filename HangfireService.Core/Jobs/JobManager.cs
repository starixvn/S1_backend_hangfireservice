using Hangfire;
using Hangfire.Dashboard.Management.Pages;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;

namespace HangfireService.Core.Jobs
{
    public static class JobManager
    {
        public static void AddRecurringJob(IServiceProvider provider)
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

                        //先設定為暫停狀態，再透過介面手動啟動
                        Pause(option.JobName);
                    }
                }
            }
        }

        public static void PauseThenRestart(string jobName, int pauseHours, string reason)
        {
            Pause(jobName);
            BackgroundJob.Schedule(() => RestartByHours(jobName, pauseHours, reason), TimeSpan.FromHours(pauseHours));
        }
        public static void Pause(string jobName)
        {
            using var connection = JobStorage.Current.GetConnection();
            if (!connection.GetRecurringJob(jobName).PauseState)
            {
                connection.SetPauseState(jobName, true);
            }      
        }

        [DisplayName("{0} restart after {1} hours, because {2}.")]
        public static void RestartByHours(string jobName, int pauseHours, string reason)
        {
            using var connection = JobStorage.Current.GetConnection();
            if (connection.GetRecurringJob(jobName).PauseState)
            {
                connection.SetPauseState(jobName, false);
            }
        }
    }
}
