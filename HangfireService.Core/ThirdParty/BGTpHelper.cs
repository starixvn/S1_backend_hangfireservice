using HangfireService.Core.DataLayers;
using HangfireService.Core.Extensions;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Finance;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Service;
using HangfireService.Core.ThirdParty.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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

        return ConvertBettransCheckData(bettrans);
    }

    private IEnumerable<BettransCheckData> ConvertBettransCheckData(IEnumerable<BettransCheckData> bettrans)
    {
        var result = new List<BettransCheckData>();
        result.AddRange(bettrans.Where(x => x.CurrencyId != 3 && x.CurrencyId != 72));

        if (bettrans.Any(x => x.CurrencyId == 3 || x.CurrencyId == 72))
        {
            var rate = _tpCurrency.First(c => c.CurrencyId == 3).ExchangeRate;
            var bettransUSD = bettrans.Where(x => x.CurrencyId == 3 || x.CurrencyId == 72);

            result.Add(new BettransCheckData()
            {
                TpId = TpId,
                CurrencyId = 3,
                TpCurrencyId = bettransUSD.First().TpCurrencyId,
                Stake = bettransUSD.Select(x => x.Stake).Sum() * rate,
                ValidBet = bettransUSD.Select(x => x.ValidBet).Sum() * rate,
                Payout = bettransUSD.Select(x => x.Payout).Sum() * rate,
                WinlostAmount = bettransUSD.Select(x => x.WinlostAmount).Sum() * rate
            });
        }

        return result;
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

    public List<FinanceCheckResult> CheckData(FinanceCheckOption checkOption, List<TpBetRecordCheck> tpBetRecord, List<BettransCheckData> bettransList, List<TpDailySummaryCheckData> tpDailyList)
    {
        var checkResult = new List<FinanceCheckResult>();
        tpBetRecord = ConvertTpBetRecordUSD(tpBetRecord);
        if (bettransList.Any())
        {
            var bettransResult = bettransList.Select(bettrans
                => bettrans.CheckData(FinanceCheckType.結算資料與交易資料檢查, tpBetRecord.FirstOrDefault(tpBetRecord => tpBetRecord.CurrencyId == bettrans.CurrencyId)));
            checkResult.AddRange(bettransResult);
        }

        if (tpDailyList.Any())
        {
            var tpResult = tpDailyList.Select(tpDaily
                => tpDaily.CheckData(FinanceCheckType.結算資料與第三方匯總檢查, tpBetRecord.FirstOrDefault(tpBetRecord => tpBetRecord.CurrencyId == tpDaily.CurrencyId)));
            checkResult.AddRange(tpResult);
        }

        if (bettransList.Any() && tpDailyList.Any())
        {
            var financeResult = bettransList.Select(bettrans
                => bettrans.CheckData(FinanceCheckType.交易資料與第三方匯總檢查, tpDailyList.FirstOrDefault(tp => tp.CurrencyId == bettrans.CurrencyId)));
            checkResult.AddRange(financeResult);
        }

        if (checkResult.Any())
        {
            var data = new
            {
                TpId,
                tpBetRecord,
                tpDailyList,
                bettransList,
            };
            _logger.LogError($"[{checkOption.BeginTime:yyyy-MM-dd}]:{JsonConvert.SerializeObject(data)}");
        }

        return checkResult;
    }

    private List<TpBetRecordCheck> ConvertTpBetRecordUSD(List<TpBetRecordCheck> tpBetRecords)
    {
        var result = new List<TpBetRecordCheck>();
        result.AddRange(tpBetRecords.Where(x => x.CurrencyId != 3 && x.CurrencyId != 72));

        if (tpBetRecords.Any(x => x.CurrencyId == 3 || x.CurrencyId == 72))
        {
            var tpBetRecordsUSD = tpBetRecords.Where(x => x.CurrencyId == 3 || x.CurrencyId == 72);

            result.Add(new TpBetRecordCheck()
            {
                TpId = TpId,
                CurrencyId = 3,
                Stake = tpBetRecordsUSD.Select(x => x.Stake).Sum(),
                ValidBet = tpBetRecordsUSD.Select(x => x.ValidBet).Sum(),
                Revenue = tpBetRecordsUSD.Select(x => x.Revenue).Sum(),
                WinlostAmount = tpBetRecordsUSD.Select(x => x.WinlostAmount).Sum(),
                ProgressiveBet = tpBetRecordsUSD.Select(x => x.ProgressiveBet).Sum(),
                ProgressiveWin = tpBetRecordsUSD.Select(x => x.ProgressiveWin).Sum()
            });
        }

        return result;
    }
}
