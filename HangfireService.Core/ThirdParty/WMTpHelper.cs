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
    internal class WMTpHelper : BaseTpHelper, IFinanceTpHelper
    {
        public int TpId => 20;

        private readonly ReportDBDataLayer _reportDbDataLayer;
        private readonly IEnumerable<TpCurrency> _tpCurrency;
        private readonly MySqlClient _wagerClient;
        private readonly MySqlClient _dbClient;
        private readonly FincanceService _fincanceService;

        public WMTpHelper(IServiceProvider provider) : base(provider)
        {
            _reportDbDataLayer = provider.GetRequiredService<ReportDBDataLayer>();

            _tpCurrency = _reportDbDataLayer.GetTpCurrency(TpId);

            _wagerClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);

            _dbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);

            _fincanceService = provider.GetRequiredService<FincanceService>();
        }

        public async Task<IEnumerable<BettransCheckData>> GetBettransCheckData(FinanceCheckOption checkOption)
        {
            var BeginTime = checkOption.BeginTime.GetUnixTimeByDay();
            var EndTime = checkOption.EndTime.GetUnixTimeByDay();

            var bettrans = await _dbClient.QueryAsync<BettransCheckData>("Finance_BettransLC_WM_Summary_Get", new { _BeginTime = BeginTime, _EndTime = EndTime });
            foreach (var item in bettrans)
            {
                item.ValidBet = item.Stake;
                item.TpId = TpId;
            }
            return bettrans;
        }

        public async Task<IEnumerable<TpDailySummaryCheckData>> GetTpDailySummaryCheckData(FinanceCheckOption checkOption)
        {
            var tpDailyList = await _fincanceService.GetTpDailySummary((int)SummaryType.TpBetRecord, TpId, checkOption.BeginTime, checkOption.EndTime);

            if (!tpDailyList.Any())
            {
                // WM無報表API 改取Wager總計存放使用
                var BeginTime = checkOption.BeginTime.GetUnixTimeByDay();
                var EndTime = checkOption.EndTime.GetUnixTimeByDay();
                tpDailyList = await _wagerClient.QueryAsync<TpDailySummaryCheckData>("Finance_TpWagerWM_Summary_Get", new { _BeginTime = BeginTime, _EndTime = EndTime });
                if (tpDailyList.Any())
                {
                    foreach (var tpDaily in tpDailyList)
                    {
                        var rate = _tpCurrency.First(c => c.TpCurrencyId == tpDaily.TpCurrencyId).ExchangeRate;
                        tpDaily.Stake *= rate;
                        tpDaily.ValidBet *= rate;
                        tpDaily.WinlostAmount *= rate;
                        tpDaily.Payout *= rate;
                        tpDaily.TpId = TpId;
                        tpDaily.CurrencyId = _tpCurrency.First(c => c.TpCurrencyId == tpDaily.TpCurrencyId).CurrencyId;
                    }
                    // 存放至TpDailySummary
                    SetTpDailySummaryCheckDataByTpId(checkOption.BeginTime, tpDailyList);
                }
            }

            return tpDailyList;
        }

        public List<FinanceCheckResult> CheckData(FinanceCheckOption checkOption, List<TpBetRecordCheck> tpBetRecord, List<BettransCheckData> bettransList, List<TpDailySummaryCheckData> tpDailyList)
        {
            // 交易資料 ValidBet 設為 結算資料 ValidBet，因交易資料無法計算出ValidBet
            foreach (var tpBettransData in bettransList)
            {
                tpBettransData.ValidBet = tpBetRecord.First(x => x.CurrencyId == tpBettransData.CurrencyId).ValidBet;
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


        private int SetTpDailySummaryCheckDataByTpId(DateTime SummaryDate, IEnumerable<TpDailySummaryCheckData> tpDailyList)
        {
            var dailyData = new List<FincanceDailySummary>();
            foreach (var item in tpDailyList)
            {
                dailyData.Add(new FincanceDailySummary()
                {
                    SummaryType = SummaryType.TpBetRecord,
                    SummaryDate = SummaryDate,
                    TpId = TpId,
                    CurrencyId = item.CurrencyId,
                    Stake = item.Stake,
                    ValidBet = item.ValidBet,
                    Payout = item.Payout,
                    WinlostAmount = item.WinlostAmount
                });
            }
            return _fincanceService.SetFinanceTpDailySummary(dailyData);
        }
    }
}
