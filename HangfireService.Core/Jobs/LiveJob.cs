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
        private const string Name = "LiveJob";

        private readonly ILogger<LiveJob> _logger;
        
        public LiveJob(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<LiveJob>>();       
            JobDetailOption = provider.GetRequiredService<List<JobDetailOption>>().First(item => item.Name == Name);
        }
        public JobDetailOption JobDetailOption { get; set; }

        public Task Execute()
        {
            var jobGuid = Guid.NewGuid();
            LogContext.PushProperty("JobGuid", jobGuid);
            LogContext.PushProperty("Job", Name);
            try
            {
                _logger.LogInformation($"{jobGuid} || {DateTime.Now:HH:mm:ss} || {Name} running......");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return Task.CompletedTask;
        }

    }
}
