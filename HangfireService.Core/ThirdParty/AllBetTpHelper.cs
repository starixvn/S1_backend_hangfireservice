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
    internal class AllBetTpHelper : BaseTpHelper, IFinanceTpHelper
    {
        public bool CheckTransId => false;
        public bool BindCommonCurrency => true;

        public TimeOption TimeOptionId => TimeOption.TransTime;

        public int TpId => 12;

        private readonly ReportDBDataLayer _reportDbDataLayer;
        private readonly IEnumerable<TpCurrency> _tpCurrency;
        private readonly MySqlClient _wagerClient;
        private readonly FincanceService _fincanceService;
        // 對方報表時間為2022-07-26 12:00:00+8 ~ 2022-07-27 12:00:00+8 ，轉為UTC+0為2022-07-26 04:00:00+0
        private readonly int utcTime = 4;

        public AllBetTpHelper(IServiceProvider provider) : base(provider)
        {
            _reportDbDataLayer = provider.GetRequiredService<ReportDBDataLayer>();

            _tpCurrency = _reportDbDataLayer.GetTpCurrency(TpId);

            _wagerClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);

            _fincanceService = provider.GetRequiredService<FincanceService>();
        }

        public async Task<IEnumerable<BettransCheckData>> GetBettransCheckData(FinanceCheckOption checkOption)
        {
            var BeginTime = (long)(checkOption.BeginTime.AddHours(utcTime).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            var EndTime = (long)(checkOption.EndTime.AddHours(utcTime).AddSeconds(-1).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            var bettrans = await _wagerClient.QueryAsync<BettransCheckData>("Finance_TpWagerAllBet_Summary_Get", new { _BeginTime = BeginTime, _EndTime = EndTime });
            var aTpCurrency = _tpCurrency.Select(i => i.TpCurrencyId).ToList();
            foreach (var item in bettrans)
            {
                if (!aTpCurrency.Contains(item.TpCurrencyId))
                {
                    continue;
                }
                var rate = _tpCurrency.First(c => c.TpCurrencyId == item.TpCurrencyId).ExchangeRate;
                item.CurrencyId = _tpCurrency.First(c => c.TpCurrencyId == item.TpCurrencyId).CurrencyId;
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

        public List<FinanceCheckResult> CheckData(FinanceCheckOption checkOption, List<TpBetRecordCheck> tpBetRecord, List<BettransCheckData> bettransList, List<TpDailySummaryCheckData> tpDailyList)
        {
            // 對方報表API為下注時間 需改用TpBetRecord.TransTime檢查
            tpBetRecord = _reportDbDataLayer.GetTpBetRecordCheckDataByTpId(TpId, checkOption.BeginTime.AddHours(utcTime), checkOption.EndTime.AddHours(utcTime), TicketStatusExcludeOption.Cancel, TimeOption.TransTime).ToList();
            tpBetRecord = ProcessTpBetRecordSubCurrencyToMainCurrency(tpBetRecord);

            // 第三方資料 ValidBet 設為 結算資料 ValidBet，因第三方資料無法計算出ValidBet
            foreach (var tpDailyData in tpDailyList)
            {
                tpDailyData.ValidBet = tpBetRecord.First(x => x.CurrencyId == tpDailyData.CurrencyId).ValidBet;
            }

            var checkResult = new List<FinanceCheckResult>();
            if (bettransList.Any())
            {
                var bettransResult = bettransList.Select(bettrans
                    => bettrans.CheckData(FinanceCheckType.結算資料與交易資料檢查, tpBetRecord.FirstOrDefault(tpBetRecordTransTimeData => tpBetRecordTransTimeData.CurrencyId == bettrans.CurrencyId)));
                checkResult.AddRange(bettransResult);
            }

            if (tpDailyList.Any())
            {
                var tpResult = tpDailyList.Select(tpDaily
                    => tpDaily.CheckData(FinanceCheckType.結算資料與第三方匯總檢查, tpBetRecord.FirstOrDefault(tpBetRecordTransTimeData => tpBetRecordTransTimeData.CurrencyId == tpDaily.CurrencyId)));
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
