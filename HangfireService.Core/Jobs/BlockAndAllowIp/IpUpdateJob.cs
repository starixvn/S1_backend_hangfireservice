
using Hangfire.Server;
using Hangfire;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Service;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.ComponentModel;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Extensions;
using HangfireService.Core.Helpers;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Options;
using Newtonsoft.Json;

namespace HangfireService.Core.Jobs.BlockAndAllowIp
{
    internal class IpUpdateJob : BaseJob, IScheduledJob
    {
        private readonly AllowIpService _allowIpService;
        private readonly BlockIpService _blockIpService;

        public IpUpdateJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _allowIpService = provider.GetRequiredService<AllowIpService>();
            _blockIpService = provider.GetRequiredService<BlockIpService>();
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName,
            JobOption jobOption,
            PerformContext context,
            IJobCancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");
            try
            {
                if (jobName is "BlockIpUpdateJob")
                    await _blockIpService.RunJobAsync();
                else
                    await _allowIpService.RunJobAsync();
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
