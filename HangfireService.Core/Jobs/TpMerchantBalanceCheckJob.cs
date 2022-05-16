using Hangfire;
using Hangfire.Server;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Service;
using Microsoft.Extensions.Logging;

namespace HangfireService.Core.Jobs
{
    public class TpMerchantBalanceCheckJob : BaseJob, IScheduledJob
    {
        private readonly TpMerchantBalanceCheckService _tpMerchantBalanceCheckService;

        public TpMerchantBalanceCheckJob(IServiceProvider provider) : base(provider)
        {
            _tpMerchantBalanceCheckService = provider.GetRequiredService<TpMerchantBalanceCheckService>();
        }

        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");

            await _tpMerchantBalanceCheckService.RunJobAsync();

            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} End......");
            return true;
        }
    }
}
