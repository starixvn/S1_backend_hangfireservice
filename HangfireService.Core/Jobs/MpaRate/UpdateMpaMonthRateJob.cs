
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

namespace HangfireService.Core.Jobs.BlockAndAllowIp
{
    [ManagementPage("UpdateMpaMonthRateJob", queue: "report")]
    internal class UpdateMpaMonthRateJob : BaseJob, IScheduledJob
    {
        private readonly MpaUpdateMonthRateService _mpaUpdateMonthRateService;


        public UpdateMpaMonthRateJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _mpaUpdateMonthRateService = provider.GetRequiredService<MpaUpdateMonthRateService>();
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context,
            IJobCancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");

            await _mpaUpdateMonthRateService.RunJobAsync(false);

            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} End......");

            return true;
        }

        [Job]
        [DisplayName("reUpdateMpaMonthRateJob")]
        public object Run([DisplayData("reRunMonth", placeholderText: "yyyy-MM, 0 = UpdateBefore")] string reRunMonth,
            PerformContext? context = null,
            IJobCancellationToken? cancellationToken = null)
        {
            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || reUpdateMpaMonthRateJob running......");

            _ = _mpaUpdateMonthRateService.RunJobAsync(true, reRunMonth).Result;

            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || reUpdateMpaMonthRateJob End......");

            return true;
        }
    }
}
