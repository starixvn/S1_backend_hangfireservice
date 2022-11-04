using HangfireService.Core.DataLayers;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Finance;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Service;
using HangfireService.Core.ThirdParty.Interfaces;
using Microsoft.Extensions.Logging;

namespace HangfireService.Core.ThirdParty
{
    public class NineWTpHelper : BaseTpHelper, IFinanceTpHelper
    {
        public int TpId { get; } = 31;
        private readonly IEnumerable<TpCurrency> _tpCurrency;
        private readonly MySqlClient _wagerDbClient;
        private readonly FincanceService _fincanceService;

        public NineWTpHelper(IServiceProvider provider) : base(provider)
        {
            _tpCurrency = provider.GetRequiredService<ReportDBDataLayer>().GetTpCurrency(TpId);

            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);

            _fincanceService = provider.GetRequiredService<FincanceService>();
        }

        public async Task<IEnumerable<BettransCheckData>> GetBettransCheckData(FinanceCheckOption checkOption)
        {
            var BeginTime = new DateTimeOffset(checkOption.BeginTime).ToUnixTimeSeconds();
            var EndTime = new DateTimeOffset(checkOption.EndTime).ToUnixTimeSeconds();
            var bettrans = await _wagerDbClient.QueryAsync<BettransCheckData>("Finance_TpWager9W_Summary_Get", new { BeginTime, EndTime });
            //foreach (var item in bettrans)
            //{
            //    item.CurrencyId = _tpCurrency.First(c => c.TpCurrencyId == item.TpCurrencyId).CurrencyId;
            //}
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
                tpDaily.WinlostAmount *= rate;
                tpDaily.Revenue *= rate;
                tpDaily.ProgressiveWin *= rate;
                tpDaily.ProgressiveWin *= rate;
                tpDaily.Payout *= rate;
                tpDaily.Reward *= rate;
            }
            return tpDailyList;
        }

        public List<FinanceCheckResult> CheckData(FinanceCheckOption checkOption, List<TpBetRecordCheck> tpBetRecord, List<BettransCheckData> bettransList, List<TpDailySummaryCheckData> tpDailyList)
        {
            //排除9W Revenue金額
            foreach (var tpDaily in tpDailyList)
            {
                tpDaily.WinlostAmount -= tpDaily.Revenue;
            }

            var checkResult = new List<FinanceCheckResult>();
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
                _logger.LogError($"[{checkOption.BeginTime:yyyy-MM-dd}]:{ToJson(data)}");
            }
            return checkResult;
        }
    }
}
