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
    internal class One789TpHelper : BaseTpHelper, IFinanceTpHelper
    {
        public bool CheckTransId => true;

        public TimeOption TimeOptionId => TimeOption.TransTime;

        public int TpId => 23;

        private readonly ReportDBDataLayer _reportDbDataLayer;
        private readonly IEnumerable<TpCurrency> _tpCurrency;
        private readonly MySqlClient _dbClient;
        private readonly MySqlClient _wagerClient;
        private readonly FincanceService _fincanceService;

        public One789TpHelper(IServiceProvider provider) : base(provider)
        {
            _reportDbDataLayer = provider.GetRequiredService<ReportDBDataLayer>();

            _tpCurrency = _reportDbDataLayer.GetTpCurrency(TpId);

            _dbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);

            _wagerClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);

            _fincanceService = provider.GetRequiredService<FincanceService>();
        }

        public async Task<IEnumerable<BettransCheckData>> GetBettransCheckData(FinanceCheckOption checkOption)
        {
            var BeginTime = checkOption.BeginTime.GetUnixTimeByDay();
            var EndTime = checkOption.EndTime.GetUnixTimeByDay();

            var tasks = new List<Task<IEnumerable<BettransCheckData>>>
            {
                _dbClient.QueryAsync<BettransCheckData>("Finance_BettransLT_One789_Summary_Get", new { _BeginTime = BeginTime, _EndTime = EndTime }, 120),
                //_dbClient.QueryAsync<BettransCheckData>("Finance_BettransLT_One789_789_Summary_Get", new { _BeginTime = BeginTime, _EndTime = EndTime }, 120),
                _dbClient.QueryAsync<BettransCheckData>("Finance_BettransLC_One789_Summary_Get", new { _BeginTime = BeginTime, _EndTime = EndTime }, 180)
            };

            await Task.WhenAll(tasks);

            var CheckData = new List<BettransCheckData>();
            foreach (var item in tasks)
            {
                CheckData.AddRange(item.Result);
            }

            //使用CurrencyId組合資料
            var bettransData = CheckData
                .GroupBy(i => i.CurrencyId)
                .Select(i => new BettransCheckData
                {
                    TpId = TpId,
                    TpCurrencyId = _tpCurrency.First(c => c.CurrencyId == i.Key).TpCurrencyId,
                    CurrencyId = i.Key,
                    Stake = i.Sum(i => i.Stake) * _tpCurrency.First(c => c.CurrencyId == i.Key).ExchangeRate,
                    ValidBet = i.Sum(i => i.ValidBet) * _tpCurrency.First(c => c.CurrencyId == i.Key).ExchangeRate,
                    Payout = i.Sum(i => i.Payout) * _tpCurrency.First(c => c.CurrencyId == i.Key).ExchangeRate,
                    WinlostAmount = i.Sum(i => i.WinlostAmount) * _tpCurrency.First(c => c.CurrencyId == i.Key).ExchangeRate,
                    Revenue = 0,
                    ProgressiveBet = 0,
                    ProgressiveWin = 0,
                });

            return bettransData;
        }

        public async Task<IEnumerable<TpDailySummaryCheckData>> GetTpDailySummaryCheckData(FinanceCheckOption checkOption)
        {
            // One789無報表API 資料為Wager總計
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
            //  改用TpBetRecord.TransTime檢查
            tpBetRecord = _reportDbDataLayer.GetTpBetRecordCheckDataByTpId(TpId, checkOption.BeginTime, checkOption.EndTime, TicketStatusExcludeOption.CancelAndUnsettle, TimeOption.TransTime).ToList();

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

        public async Task<IEnumerable<BettransCheckTransIdData>> GetBettransCheckTransIdData(FinanceCheckTransIdOption checkOption)
        {
            var BeginTime = checkOption.BeginTime.GetUnixTime();
            var EndTime = checkOption.EndTime.GetUnixTime();

            var tasks = new List<Task<IEnumerable<BettransCheckTransIdData>>>
            {
                _wagerClient.QueryAsync<BettransCheckTransIdData>("Finance_TpWagerOne789_LC_Check_TransId_Get", new { _BeginTime = BeginTime, _EndTime = EndTime }, 120),
                _wagerClient.QueryAsync<BettransCheckTransIdData>("Finance_TpWagerOne789_LT_Check_TransId_Get", new { _BeginTime = BeginTime, _EndTime = EndTime }, 120)
            };

            await Task.WhenAll(tasks);

            var CheckData = new List<BettransCheckTransIdData>();
            foreach (var item in tasks)
            {
                CheckData.AddRange(item.Result);
            }

            return (from bettrans in CheckData
                    let exchangeRate = _tpCurrency.First(c => c.CurrencyId == bettrans.CurrencyId).ExchangeRate
                    select new BettransCheckTransIdData
                    {
                        TpId = TpId,
                        TransId = bettrans.TransId,
                        CurrencyId = bettrans.CurrencyId,
                        Stake = bettrans.Stake * exchangeRate,
                        ValidBet = bettrans.ValidBet * exchangeRate,
                        Payout = 0,
                        WinlostAmount = bettrans.WinlostAmount * exchangeRate,
                        Revenue = 0,
                        ProgressiveBet = 0,
                        ProgressiveWin = 0,
                    }).ToList();
        }
    }
}
