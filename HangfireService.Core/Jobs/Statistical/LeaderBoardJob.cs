using Hangfire.Server;
using Hangfire;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Service;
using System.ComponentModel;
using HangfireService.Core.Models.Jobs;
using Microsoft.Extensions.Logging;

namespace HangfireService.Core.Jobs;
public class LeaderBoardJob : BaseJob, IScheduledJob
{
    private readonly LeaderBoardService _leaderBoardService;

    public LeaderBoardJob(IServiceProvider provider) : base(provider)
    {
        _leaderBoardService = provider.GetRequiredService<LeaderBoardService>();
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
            await _leaderBoardService.RunJobAsync(jobName);
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