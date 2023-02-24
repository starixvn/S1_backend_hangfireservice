using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Options;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

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

    public async Task<int> RunJobAsync(string jobName, LeaderBoardOption option)
    {
       return await LeaderBoardStatistics(option, jobName);      
    }

    private async Task<int> LeaderBoardStatistics(LeaderBoardOption option, string jobName)
    {
        var leaderBoardList = await GetTpBetRecordForLeadBoard(option);
        _logger.LogInformation("leaderBoardList:" + JsonConvert.SerializeObject(leaderBoardList));
        var dd = leaderBoardList.ToDictionary(x => $"{x.CategoryId}_{x.CustId}", x => x);
        var extraPointList = await GetLeaderBoardExtraPoint(option);
        _logger.LogInformation("extraPointList:" + JsonConvert.SerializeObject(extraPointList));
        foreach (var extraPoint in extraPointList)
        {
            var key = $"{extraPoint.CategoryId}_{extraPoint.CustId}";
            if (dd.ContainsKey(key))
            {
                dd[key].Turnover += extraPoint.Turnover;
                dd[key].WinlostAmount += extraPoint.WinlostAmount;
            }
            else
            {
                dd.Add(key, extraPoint);
            }           
        }
        var result = dd.Select(i => i.Value).ToList();
        _logger.LogInformation("result:" + JsonConvert.SerializeObject(result));
        return await SetLeaderBoard(result, jobName);
    }

    private async Task<IEnumerable<GetTpBetRecordForLeadBoardDto>> GetTpBetRecordForLeadBoard(LeaderBoardOption option)
    {
        return await _reportDBDataLayer.GetTpBetRecordForLeadBoard(option);
    }

    private async Task<IEnumerable<GetTpBetRecordForLeadBoardDto>> GetLeaderBoardExtraPoint(LeaderBoardOption option)
    {
        return string.IsNullOrWhiteSpace(option.ExtraPointSP) ?
            await _reportDBDataLayer.GetLeaderBoardExtraPoint(option, "Service_LeaderboardExtraPoint_Get") :
            await _reportDBDataLayer.GetLeaderBoardExtraPoint(option, option.ExtraPointSP);
    }

    private async Task<int> SetLeaderBoard(IEnumerable<GetTpBetRecordForLeadBoardDto> leadBoardDtos, string jobName)
    {
        await using var conn = _mainDBDataLayer.GetMySqlConnection();
        conn.Open();
        var trans = await conn.BeginTransactionAsync();
        var result = 0;
        try
        {
            
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
        return result;
    }
}
