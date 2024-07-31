using System.ComponentModel;
using Hangfire;
using Hangfire.Server;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Service;
using Microsoft.Extensions.Logging;

using Serilog.Context;

namespace HangfireService.Core.Jobs
{
    public class HotEventInfoJob : BaseJob, IScheduledJob
    {
        private readonly HotEventService _hotEventService;

        public HotEventInfoJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _hotEventService = provider.GetRequiredService<HotEventService>();
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");
            try
            {
                await _hotEventService.HotEventInfoJobAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message, $"{DateTime.Now:HH:mm:ss} || {jobName} exception......");
                return false;
            }
            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} End......");
            return true;
        }
        
    }
}
