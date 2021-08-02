using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HangfireService.Core.Jobs
{
    public class LiveJob : IScheduledJob
    {
        private readonly ILogger<LiveJob> _logger;

        public string ClassName { get; set; }       


        public LiveJob(IServiceProvider provider)
        {
            ClassName = GetType().Name; 
            _logger = provider.GetRequiredService<ILogger<LiveJob>>();       
        }

        public Task Execute(JobDetail jobDetail, string[] args)
        {
            var jobGuid = Guid.NewGuid();
            LogContext.PushProperty("JobGuid", jobGuid);
            LogContext.PushProperty("Job", jobDetail.JobName);
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

    }
}
