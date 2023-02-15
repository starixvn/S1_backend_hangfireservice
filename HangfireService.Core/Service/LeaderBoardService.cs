using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs;
using HangfireService.Core.Models.DBs;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace HangfireService.Core.Service;

public class LeaderBoardService
{
    private readonly MainDBDataLayer _mainDBDataLayer;
    private readonly ReportDBDataLayer _reportDBDataLayer;
    private readonly ILogger<BaseJob> _logger;

    public LeaderBoardService(IServiceProvider provider)
    {
        _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
        _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
        _logger = provider.GetRequiredService<ILogger<BaseJob>>();
    }

    public async Task RunJobAsync(string jobName)
    {
        foreach (var @event in await GetLeaderBoardEvent())
            await LeaderBoardStatistics(@event, jobName);
        await SetLeaderBoardEventOverTime();
        await Task.CompletedTask;
    }

    private async Task<IEnumerable<LeaderboardEventDto>> GetLeaderBoardEvent()
        => await _reportDBDataLayer.GetLeaderBoardEvent();

    private async Task LeaderBoardStatistics(LeaderboardEventDto @event,string jobName)
    {
        var leaderBoardList = GetTpBetRecordForLeadBoard(0, @event).Result.ToList();
        var dd = leaderBoardList.ToDictionary(x => $"{x.CategoryId}_{x.CustId}", x => x);

        foreach (var relayItem in await GetTpBetRecordForLeadBoard(1, @event))
        {
            if (dd.ContainsKey($"{relayItem.CategoryId}_{relayItem.CustId}"))
            {
                dd[$"{relayItem.CategoryId}_{relayItem.CustId}"].Turnover += relayItem.Turnover;
                dd[$"{relayItem.CategoryId}_{relayItem.CustId}"].WinlostAmount += relayItem.WinlostAmount;
            }
            else
                leaderBoardList.Add(relayItem);
        }

        foreach (var extraPoint in await GetLeaderBoardExtraPoint(@event))
        {
            if (dd.ContainsKey($"{extraPoint.CategoryId}_{extraPoint.CustId}"))
            {
                dd[$"{extraPoint.CategoryId}_{extraPoint.CustId}"].Turnover += extraPoint.Turnover;
                dd[$"{extraPoint.CategoryId}_{extraPoint.CustId}"].WinlostAmount += extraPoint.WinlostAmount;
            }
            else
                leaderBoardList.Add(extraPoint);
        }
        await SetLeaderBoard(leaderBoardList, jobName);
        await Task.CompletedTask;
    }

    private async Task<IEnumerable<GetTpBetRecordForLeadBoardDto>> GetTpBetRecordForLeadBoard(int isRelay, LeaderboardEventDto eventDto)
    => await _reportDBDataLayer.GetTpBetRecordForLeadBoard(isRelay, eventDto);

    private async Task<IEnumerable<GetTpBetRecordForLeadBoardDto>> GetLeaderBoardExtraPoint(LeaderboardEventDto eventDto)
    {
        if (!string.IsNullOrWhiteSpace(eventDto.ExtraPointSP))
            return await _reportDBDataLayer.GetLeaderBoardExtraPoint(eventDto, eventDto.ExtraPointSP);
        else
            return await _reportDBDataLayer.GetLeaderBoardExtraPoint(eventDto, "Service_LeaderboardExtraPoint_Get");
    }

    private async Task SetLeaderBoard(IEnumerable<GetTpBetRecordForLeadBoardDto> leadBoardDtos,string jobName)
    {
        //至多重新執行 N 次
        int RtyTimes = 2;
        for (int i = 0; i < RtyTimes; ++i)
        {
            try
            {
                await _mainDBDataLayer.SetLeaderBoard(leadBoardDtos);
                break;
            }
            catch (Exception ex)
            {
                if (i == RtyTimes - 1)
                {
                    _logger.LogError(ex, ex.Message, $"{DateTime.Now:HH:mm:ss} || {jobName} exception......");
                    break;
                }
            }
        }
    }

    private async Task<int> SetLeaderBoardEventOverTime()
 => await _mainDBDataLayer.SetLeaderBoardEventOverTime();
}
