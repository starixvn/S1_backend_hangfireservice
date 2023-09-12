using HangfireService.Core.DataLayers;
using HangfireService.Core.Extensions;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Finance;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Service;
using HangfireService.Core.ThirdParty.Interfaces;
using Microsoft.Extensions.Logging;

namespace HangfireService.Core.ThirdParty
{
    internal class NextspinTpHelper : BaseTpHelper, IFinanceTpHelper
    {
        public bool CheckTransId => true;
        public bool BindCommonCurrency => true;
        public TimeOption TimeOptionId => TimeOption.TransTime;

        public int TpId => 50;

        private readonly ReportDBDataLayer _reportDbDataLayer;
        private readonly IEnumerable<TpCurrency> _tpCurrency;
        private readonly MySqlClient _wagerClient;
        private readonly FincanceService _fincanceService;
        private readonly int utcTime = -8;

        public NextspinTpHelper(IServiceProvider provider) : base(provider)
        {
            _reportDbDataLayer = provider.GetRequiredService<ReportDBDataLayer>();

            _tpCurrency = _reportDbDataLayer.GetTpCurrency(TpId);

            _wagerClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);

            _fincanceService = provider.GetRequiredService<FincanceService>();
        }

        public async Task<IEnumerable<BettransCheckData>> GetBettransCheckData(FinanceCheckOption checkOption)
        {
            var BeginTime = checkOption.BeginTime.GetUnixTimeByDay();
            var EndTime = checkOption.EndTime.GetUnixTimeByDay() - 1;
            var bettrans = await _wagerClient.QueryAsync<BettransCheckData>("Finance_TpWagerRng_Nextspin_Summary_Get", new { _BeginTime = BeginTime, _EndTime = EndTime });
            foreach (var item in bettrans)
            {
                var tpCurrency = _tpCurrency.First(c => c.TpCurrencyId == item.TpCurrencyId);
                item.TpId = TpId;
                item.CurrencyId = tpCurrency.CurrencyId;
                item.Stake *= tpCurrency.ExchangeRate;
                item.ValidBet *= tpCurrency.ExchangeRate;
                item.WinlostAmount *= tpCurrency.ExchangeRate;
                item.Payout = (item.Stake + item.WinlostAmount);
            }
            return bettrans;
        }

        public async Task<IEnumerable<TpDailySummaryCheckData>> GetTpDailySummaryCheckData(FinanceCheckOption checkOption)
        {
            // Nextspin無報表API 資料為Wager總計
            var tpDailyList = await _fincanceService.GetTpDailySummary((int)SummaryType.TpBetRecord, TpId, checkOption.BeginTime, checkOption.EndTime);
            foreach (var tpDaily in tpDailyList)
            {
                var rate = _tpCurrency.First(c => c.CurrencyId == tpDaily.CurrencyId).ExchangeRate;
                tpDaily.Stake *= rate;
                tpDaily.ValidBet *= rate;
                tpDaily.WinlostAmount *= rate;
                tpDaily.Payout *= rate;
            }
            return tpDailyList;
        }

        public List<FinanceCheckResult> CheckData(FinanceCheckOption checkOption, List<TpBetRecordCheck> tpBetRecord, List<BettransCheckData> bettransList, List<TpDailySummaryCheckData> tpDailyList)
        {
            // 改用TpBetRecord.TransTime檢查
            tpBetRecord = _reportDbDataLayer.GetTpBetRecordCheckDataByTpId(TpId, checkOption.BeginTime.AddHours(utcTime), checkOption.EndTime.AddHours(utcTime), TicketStatusExcludeOption.Cancel, TimeOption.TransTime).ToList();
            tpBetRecord = ProcessTpBetRecordSubCurrencyToMainCurrency(tpBetRecord);

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

        public async Task<IEnumerable<BettransCheckTransIdData>> GetBettransCheckTransIdData(FinanceCheckTransIdOption checkOption)
        {
            var BeginTime = checkOption.BeginTime.GetUnixTime();
            var EndTime = checkOption.EndTime.GetUnixTime() - 1;
            int Mode = (int)checkOption.Mode;
            var TransId = checkOption.TransIds;
            if (Mode == (int)ConditionOption.TransId && TransId == "[]")
            {
                return Enumerable.Empty<BettransCheckTransIdData>();
            }

            var Parameter = new { _Mode = Mode, _BeginTime = BeginTime, _EndTime = EndTime, _TransIds = TransId };
            var bettrans = await _wagerClient.QueryAsync<BettransCheckTransIdData>("Finance_TpWagerRng_Nextspin_Check_TransId_Get", Parameter);

            return (from item in bettrans
                    let exchangeRate = _tpCurrency.First(c => c.CurrencyId == item.CurrencyId).ExchangeRate
                    select new BettransCheckTransIdData
                    {
                        TpId = TpId,
                        TransId = item.TransId,
                        CurrencyId = item.CurrencyId,
                        Stake = item.Stake * exchangeRate,
                        ValidBet = item.ValidBet * exchangeRate,
                        Payout = 0,
                        WinlostAmount = item.WinlostAmount * exchangeRate,
                        Revenue = 0,
                        ProgressiveBet = 0,
                        ProgressiveWin = 0,
                    }).ToList();
        }
    }
}
