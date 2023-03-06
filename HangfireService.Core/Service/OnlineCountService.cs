using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs;
using HangfireService.Core.Models.DBs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HangfireService.Core.Service;

public class OnlineCountService
{
    private readonly MainDBDataLayer _mainDBDataLayer;
    private readonly ReportDBDataLayer _reportDBDataLayer;
    private readonly ILogger<BaseJob> _logger;

    public OnlineCountService(IServiceProvider provider)
    {
        _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
        _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
        _logger = provider.GetRequiredService<ILogger<BaseJob>>();
    }

    public async Task RunJobAsync()
    {
        await SetOnlineCount(await GetOnlineCount());
        await Task.CompletedTask;
    }

    private async Task<IEnumerable<OnlineCountDto>> GetOnlineCount()
        => await _reportDBDataLayer.GetOnlineCount();

    private async Task<int> SetOnlineCount(IEnumerable<OnlineCountDto> onlineCountDtos)
        => await _mainDBDataLayer.SetOnlineCount(onlineCountDtos);
}
