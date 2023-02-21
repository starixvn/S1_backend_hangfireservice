using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Options;
using Microsoft.Extensions.Logging;

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

    public async Task RunJobAsync(string jobName, LeaderBoardOption option)
    {
        await LeaderBoardStatistics(option, jobName);
        await Task.CompletedTask;
    }

    private async Task LeaderBoardStatistics(LeaderBoardOption option, string jobName)
    {
        var leaderBoardList = GetTpBetRecordForLeadBoard(0, option).Result.ToList();
        var dd = leaderBoardList.ToDictionary(x => $"{x.CategoryId}_{x.CustId}", x => x);

        foreach (var relayItem in await GetTpBetRecordForLeadBoard(1, option))
        {
            if (dd.ContainsKey($"{relayItem.CategoryId}_{relayItem.CustId}"))
            {
                dd[$"{relayItem.CategoryId}_{relayItem.CustId}"].Turnover += relayItem.Turnover;
                dd[$"{relayItem.CategoryId}_{relayItem.CustId}"].WinlostAmount += relayItem.WinlostAmount;
            }
            else
            {
                leaderBoardList.Add(relayItem);
            }
        }

        foreach (var extraPoint in await GetLeaderBoardExtraPoint(option))
        {
            if (dd.ContainsKey($"{extraPoint.CategoryId}_{extraPoint.CustId}"))
            {
                dd[$"{extraPoint.CategoryId}_{extraPoint.CustId}"].Turnover += extraPoint.Turnover;
                dd[$"{extraPoint.CategoryId}_{extraPoint.CustId}"].WinlostAmount += extraPoint.WinlostAmount;
            }
            else
            {
                leaderBoardList.Add(extraPoint);
            }           
        }
        await SetLeaderBoard(leaderBoardList, jobName);
        await Task.CompletedTask;
    }

    private async Task<IEnumerable<GetTpBetRecordForLeadBoardDto>> GetTpBetRecordForLeadBoard(int isRelay, LeaderBoardOption option)
        => await _reportDBDataLayer.GetTpBetRecordForLeadBoard(isRelay, option);

    private async Task<IEnumerable<GetTpBetRecordForLeadBoardDto>> GetLeaderBoardExtraPoint(LeaderBoardOption option)
    {
        return string.IsNullOrWhiteSpace(option.ExtraPointSP) ?
            await _reportDBDataLayer.GetLeaderBoardExtraPoint(option, "Service_LeaderboardExtraPoint_Get") :
            await _reportDBDataLayer.GetLeaderBoardExtraPoint(option, option.ExtraPointSP);
    }

    private async Task SetLeaderBoard(IEnumerable<GetTpBetRecordForLeadBoardDto> leadBoardDtos, string jobName)
    {
        await using var conn = _mainDBDataLayer.GetMySqlConnection();
        conn.Open();
        var trans = await conn.BeginTransactionAsync();
        try
        {
            var result = 0;
            result += await _mainDBDataLayer.SetLeaderBoard(leadBoardDtos, trans);
            if (result == leadBoardDtos.Count())
            {
                await trans.CommitAsync();
            }
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            _logger.LogError(ex, ex.Message, $"{DateTime.Now:HH:mm:ss} || {jobName} SetLeaderBoard exception......");
        }
        finally
        {
            await conn.CloseAsync();
            await conn.DisposeAsync();
        }
    }
}
