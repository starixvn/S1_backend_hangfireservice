using System.ComponentModel;

using Hangfire;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using Hangfire.Server;

using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Service;

using Microsoft.Extensions.Logging;

using Serilog.Context;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("UpdateAffiliateInfoJob", queue: "member")]
    public class UpdateAffiliateInfoJob : BaseJob, IScheduledJob
    {
        private readonly UpdateAffiliateInfoService _updateAffiliateInfoService;

        public UpdateAffiliateInfoJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _updateAffiliateInfoService = provider.GetRequiredService<UpdateAffiliateInfoService>();
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context,
            IJobCancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");

            var result = await _updateAffiliateInfoService.RunJobAsync();

            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} End......");

            return $"UpdateExchangeRateJob ExecuteAsync：{result}";
        }

        [Job]
        [DisplayName("reUpdateAffiliateInfoJob")]
        public object Run(PerformContext? context = null, IJobCancellationToken? cancellationToken = null)
        {
            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || reUpdateAffiliateInfoJob running......");

            var result = _updateAffiliateInfoService.RunJobAsync().Result;

            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || reUpdateAffiliateInfoJob End......");

            return result;
        }
    }
}
