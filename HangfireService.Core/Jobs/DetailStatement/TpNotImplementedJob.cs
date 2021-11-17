using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.ThirdParty;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Newtonsoft.Json;
using System.ComponentModel;
using Hangfire.Server;
using Hangfire;

namespace HangfireService.Core.Jobs
{
    public class TpNotImplementedJob : BaseJob, IScheduledJob
    {
        private readonly ThirdPartyManager _thirdPartyManager;

        public TpNotImplementedJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name); 
            _thirdPartyManager = provider.GetRequiredService<ThirdPartyManager>();
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption , PerformContext context, IJobCancellationToken cancellationToken)
        {
            var jobId = context.BackgroundJob.Id;
            LogContext.PushProperty("jobId", jobId);

            try
            {
                _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} {jobId} running......");
                var excludesTpList = jobOption.Parameters.Select(item => Convert.ToInt32(item.Value));
                var result = await _thirdPartyManager.TpNotImplemented(excludesTpList);

                return JsonConvert.SerializeObject(new
                {
                    Success = result,
                });
            }
            catch (Exception ex)
            {            
                _logger.LogError(ex, ex.Message);
                return JsonConvert.SerializeObject(ex);
            }
          
        }
    }
}
