using HangfireService.Core.DataLayers;
using HangfireService.Core.Extensions;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Finance;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Service;
using HangfireService.Core.ThirdParty.Interfaces;
using Microsoft.Extensions.Logging;

namespace HangfireService.Core.ThirdParty;

internal class BGTpHelper : BaseTpHelper, IFinanceTpHelper
{
    public bool CheckTransId => false;

    public TimeOption TimeOptionId => TimeOption.SettledTime;

    public int TpId => 47;

    private readonly ReportDBDataLayer _reportDbDataLayer;
    private readonly IEnumerable<TpCurrency> _tpCurrency;
    private readonly MySqlClient _dbClient;
    private readonly FincanceService _fincanceService;
    private readonly int utcTime = -4;
    public BGTpHelper(IServiceProvider provider) : base(provider)
    {
        _reportDbDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
        _tpCurrency = _reportDbDataLayer.GetTpCurrency(TpId);
        _dbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        _fincanceService = provider.GetRequiredService<FincanceService>();
    }

    public async Task<IEnumerable<BettransCheckData>> GetBettransCheckData(FinanceCheckOption checkOption)
    {
        var BeginTime = (long)(checkOption.BeginTime.AddHours(utcTime).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        var EndTime = (long)(checkOption.EndTime.AddHours(utcTime).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        var bettrans = await _dbClient.QueryAsync<BettransCheckData>("Finance_TpWagerLC_BG_Summary_Get", new { _BeginTime = BeginTime, _EndTime = EndTime });
        foreach (var item in bettrans)
        {
            var rate = _tpCurrency.First(c => c.CurrencyId == item.CurrencyId).ExchangeRate;
            item.TpId = TpId;
            item.Stake *= rate;
            item.ValidBet *= rate;
            item.Payout *= rate;
            item.WinlostAmount *= rate;
        }
        return bettrans;
    }

    public async Task<IEnumerable<TpDailySummaryCheckData>> GetTpDailySummaryCheckData(FinanceCheckOption checkOption)
    {
        var tpDailyList = await _fincanceService.GetTpDailySummary((int)SummaryType.TpBetRecord, TpId, checkOption.BeginTime, checkOption.EndTime);
        foreach (var tpDaily in tpDailyList)
        {
            var rate = _tpCurrency.First(c => c.CurrencyId == tpDaily.CurrencyId).ExchangeRate;
            tpDaily.Stake *= rate;
            tpDaily.ValidBet *= rate;
            tpDaily.Payout *= rate;
            tpDaily.WinlostAmount *= rate;
        }
        return tpDailyList;
    }
}
