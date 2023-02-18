using Hangfire;
using Hangfire.Server;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Service;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace HangfireService.Core.Jobs;

internal class OnlineCountJob : BaseJob, IScheduledJob
{
    private readonly OnlineCountService _onlineCountService;

    public OnlineCountJob(IServiceProvider provider) : base(provider)
    {
        _onlineCountService = provider.GetRequiredService<OnlineCountService>();
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
            await _onlineCountService.RunJobAsync();
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
