using Hangfire.Server;
using Hangfire;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Service;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HangfireService.Core.Jobs.Interfaces;
using Hangfire.Dashboard.Management.Metadata;
using Microsoft.Extensions.Logging;

namespace HangfireService.Core.Jobs;

[ManagementPage("CustClosingBalanceJob", queue: "statistical")]
public class CustClosingBalanceJob : BaseJob, IScheduledJob
{
    private readonly CustClosingBalanceService _custClosingBalanceService;

    public CustClosingBalanceJob(IServiceProvider provider) : base(provider)
    {
        _custClosingBalanceService = provider.GetRequiredService<CustClosingBalanceService>();
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
            await _custClosingBalanceService.RunJobAsync(jobName);
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
