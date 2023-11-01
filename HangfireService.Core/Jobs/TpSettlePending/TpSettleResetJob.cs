using System.ComponentModel;

using Hangfire;
using Hangfire.Server;

using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Serilog.Context;

namespace HangfireService.Core.Jobs
{
    public class TpSettleResetJob : BaseJob, IScheduledJob
    {
        private readonly MainDBDataLayer _mainDBDataLayer;

        public TpSettleResetJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");
                var result = await _mainDBDataLayer.ResetTpUnsettle();
                return $"TpSettle reset count: {result}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return JsonConvert.SerializeObject(ex);
            }

        }
    }
}
