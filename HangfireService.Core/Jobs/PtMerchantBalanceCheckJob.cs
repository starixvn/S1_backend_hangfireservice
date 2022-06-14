using Hangfire;
using Hangfire.Server;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Service;
using Microsoft.Extensions.Logging;

namespace HangfireService.Core.Jobs
{
    public class PtMerchantBalanceCheckJob : BaseJob, IScheduledJob
    {
        private readonly PtMerchantBalanceCheckService _ptMerchantBalanceCheckService;

        public PtMerchantBalanceCheckJob(IServiceProvider provider) : base(provider)
        {
            _ptMerchantBalanceCheckService = provider.GetRequiredService<PtMerchantBalanceCheckService>();
        }

        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");

            await _ptMerchantBalanceCheckService.RunJobAsync();

            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} End......");
            return true;
        }
    }
}
