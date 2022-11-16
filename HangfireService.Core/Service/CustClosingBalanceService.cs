
using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs;
using HangfireService.Core.Models.DBs;
using Microsoft.Extensions.Logging;

namespace HangfireService.Core.Service;

public class CustClosingBalanceService
{
    private readonly MainDBDataLayer _mainDBDataLayer;
    private readonly ILogger<BaseJob> _logger;

    public CustClosingBalanceService(IServiceProvider provider)
    {
        _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
        _logger = provider.GetRequiredService<ILogger<BaseJob>>();
    }

    public async Task RunJobAsync(string jobName)
    {
        var totalCount = await SetCustClosingBalance();
        _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} finish. affected count={totalCount}.");
        await Task.CompletedTask;
    }

    private async Task<int> SetCustClosingBalance()
        => await _mainDBDataLayer.SetCustClosingBalance();

}
