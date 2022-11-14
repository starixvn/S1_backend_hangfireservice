using HangfireService.Core.Jobs.Interfaces;
using Hangfire;
using Hangfire.Server;
using HangfireService.Core.Models.Jobs;
using System.ComponentModel;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using HangfireService.Core.Service;
using Microsoft.Extensions.Logging;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("UpdateExchangeRateJob", queue: "report")]
    public class UpdateExchangeRateJob : BaseJob, IScheduledJob
    {
        private readonly MpaUpdateExchangeRateService _mpaUpdateExchangeRateService;

        public UpdateExchangeRateJob(IServiceProvider provider) : base(provider)
        {
            this._mpaUpdateExchangeRateService = provider.GetRequiredService<MpaUpdateExchangeRateService>();
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context,
            IJobCancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");

            var result = await _mpaUpdateExchangeRateService.RunJobAsync();

            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} End......");

            return $"UpdateExchangeRateJob ExecuteAsync：{result}";
        }

        [Job]
        [DisplayName("reUpdateExchangeRateJob")]
        public object Run([DisplayData("reRunDate")] string reRunDate,
            PerformContext? context = null,
            IJobCancellationToken? cancellationToken = null)
        {
            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || reUpdateExchangeRateJob running......");

            var result = _mpaUpdateExchangeRateService.RunJobAsync(reRunDate).Result;

            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || reUpdateExchangeRateJob End......");

            return result;
        }
    }
}