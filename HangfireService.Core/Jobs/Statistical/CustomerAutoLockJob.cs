using System.ComponentModel;

using Hangfire;
using Hangfire.Server;

using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Service;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HangfireService.Core.Jobs;
public class CustomerAutoLockJob : BaseJob, IScheduledJob
{
    private readonly CustomerAutoLockService _customerAutoLockService;
    private readonly IHostEnvironment _environment;
    public CustomerAutoLockJob(IServiceProvider provider) : base(provider)
    {
        _customerAutoLockService = provider.GetRequiredService<CustomerAutoLockService>();
        _environment = provider.GetRequiredService<IHostEnvironment>();
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
            if (!_environment.IsDevelopment())
                await _customerAutoLockService.RunJobAsync();
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
