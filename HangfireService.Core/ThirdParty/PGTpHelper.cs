using System;
using HangfireService.Core.Attributes;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Enums;
using HangfireService.Core.Extensions;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Finance;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Service;
using HangfireService.Core.ThirdParty.Interfaces;

namespace HangfireService.Core.ThirdParty;

internal class PGTpHelper : BaseTpHelper, IFinanceTpHelper
{
    public bool CheckTransId => false;
    public bool BindCommonCurrency => false;
    public TimeOption TimeOptionId => TimeOption.SettledTime;
    public int TpId => (int)Tp.PG;
    private readonly PartitionService _partitionService;
    private readonly FincanceService _financeService;

    public PGTpHelper(IServiceProvider provider) : base(provider)
    {
        _partitionService = provider.GetRequiredService<PartitionService>();
        _financeService = provider.GetRequiredService<FincanceService>();

    }

    public async Task<IEnumerable<BettransCheckData>> GetBettransCheckData(FinanceCheckOption checkOption)
    {
        var beginTime = checkOption.BeginTime.GetUnixTimeByDay();
        var endTime = checkOption.EndTime.GetUnixTimeByDay() - 1;

        var tasks = new List<Task<IEnumerable<BettransCheckData>>>();
        tasks.AddRange(
            _partitionService
            .QueryAsync<BettransCheckData>(
                checkOption.BeginTime, 
                checkOption.EndTime, 
                "Finance_BettransRng_PG_Summary_Get",
                new {
                    _BeginTime = beginTime,
                    _EndTime = endTime
                })
            );
        await Task.WhenAll(tasks);

        var checkData = new List<BettransCheckData>();
        foreach (var task in tasks) {
            var data = task.Result;
            checkData.AddRange(data);
        }
        
        var bettransCheckData = checkData
            .GroupBy(item => item.CurrencyId)
            .Select(item => new BettransCheckData{
                TpId = TpId,
                CurrencyId = item.Key,
                Stake = item.Sum(i => i.Stake),
                ValidBet = item.Sum(i => i.ValidBet),
                Payout = item.Sum(i => i.Payout),
                WinlostAmount = item.Sum(i => i.WinlostAmount),
                Revenue = 0,
                ProgressiveBet = 0,
                ProgressiveWin = 0
            });
            
        return bettransCheckData;
    }

    public async Task<IEnumerable<TpDailySummaryCheckData>> GetTpDailySummaryCheckData(FinanceCheckOption checkOption)
    {
        return await _financeService.GetTpDailySummary((int)SummaryType.TpBetRecord, TpId, checkOption.BeginTime, checkOption.EndTime);
    }
}
