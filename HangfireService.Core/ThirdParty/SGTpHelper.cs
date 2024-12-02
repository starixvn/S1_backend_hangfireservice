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
    internal class SGTpHelper : BaseTpHelper, IFinanceTpHelper
    {
        public bool CheckTransId => true;
        public bool BindCommonCurrency => true;
        public TimeOption TimeOptionId => TimeOption.TransTime;

        public int TpId => 10;

        private readonly ReportDBDataLayer _reportDbDataLayer;
        private readonly IEnumerable<TpCurrency> _tpCurrency;
        private readonly MySqlClient _dbClient;
        private readonly MySqlClient _wagerClient;
        private readonly FincanceService _fincanceService;

        public SGTpHelper(IServiceProvider provider) : base(provider)
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
            var EndTime = checkOption.EndTime.GetUnixTimeByDay() - 1;

            var CheckData = new List<BettransCheckData>();

            var bettrans = await _dbClient.QueryAsync<BettransCheckData>("Finance_BettransRng_SG_Summary_Get", new { _BeginTime = BeginTime, _EndTime = EndTime });

            var wagerSlot = await _wagerClient.QueryAsync<BettransCheckData>("Finance_TpWagerRng_SG_Summary_Get", new { _BeginTime = BeginTime, _EndTime = EndTime });
            foreach (var item in wagerSlot)
            {
                var rate = _tpCurrency.First(c => c.TpCurrencyId == item.TpCurrencyId).ExchangeRate;
                item.ProgressiveBet *= rate;
                item.ProgressiveWin *= rate;
            }

            var wagerFish = await _wagerClient.QueryAsync<BettransCheckData>("Finance_TpWagerFish_SG_Summary_Get", new { _BeginTime = BeginTime, _EndTime = EndTime });
            foreach (var item in wagerFish)
            {
                var rate = _tpCurrency.First(c => c.TpCurrencyId == item.TpCurrencyId).ExchangeRate;
                item.Stake *= rate;
                item.ValidBet *= rate;
                item.WinlostAmount *= rate;
                item.ProgressiveBet *= rate;
                item.ProgressiveWin *= rate;
            }

            CheckData.AddRange(bettrans);
            CheckData.AddRange(wagerSlot);
            CheckData.AddRange(wagerFish);

            return CheckData
                .GroupBy(i => i.TpCurrencyId)
                .Select(i => new BettransCheckData
                {
                    TpId = TpId,
                    CurrencyId = _tpCurrency.First(c => c.TpCurrencyId == i.Key).CurrencyId,
                    Stake = i.Sum(i => i.Stake),
                    ValidBet = i.Sum(i => i.ValidBet),
                    Payout = i.Sum(i => i.Stake + i.WinlostAmount),
                    WinlostAmount = i.Sum(i => i.WinlostAmount),
                    Revenue = 0,
                    ProgressiveBet = i.Sum(i => i.ProgressiveBet),
                    ProgressiveWin = i.Sum(i => i.ProgressiveWin),
                });
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
                tpDaily.ProgressiveBet *= rate;
                tpDaily.ProgressiveWin *= rate;
            }
            return tpDailyList;
        }

        public List<FinanceCheckResult> CheckData(FinanceCheckOption checkOption, List<TpBetRecordCheck> tpBetRecord, List<BettransCheckData> bettransList, List<TpDailySummaryCheckData> tpDailyList)
        {
            // 對方Wager只有下注時間欄位 改用TpBetRecord.TransTime檢查
            tpBetRecord = _reportDbDataLayer.GetTpBetRecordCheckDataByTpId(TpId, checkOption.BeginTime, checkOption.EndTime, TicketStatusExcludeOption.Cancel, TimeOption.TransTime).ToList();
            tpBetRecord = ProcessTpBetRecordSubCurrencyToMainCurrency(tpBetRecord);

            var checkResult = new List<FinanceCheckResult>();
            if (bettransList.Any())
            {
                var bettransResult = bettransList.Select(bettrans
                    => bettrans.CheckData(FinanceCheckType.CompareTpBetRecordSumAndBettrans, tpBetRecord.FirstOrDefault(tpBetRecordTransTimeData => tpBetRecordTransTimeData.CurrencyId == bettrans.CurrencyId)));
                checkResult.AddRange(bettransResult);
            }

            if (tpDailyList.Any())
            {
                var tpResult = tpDailyList.Select(tpDaily
                    => tpDaily.CheckData(FinanceCheckType.CompareTpDailySummaryAndTpBetRecordSum, tpBetRecord.FirstOrDefault(tpBetRecordTransTimeData => tpBetRecordTransTimeData.CurrencyId == tpDaily.CurrencyId)));
                checkResult.AddRange(tpResult);
            }

            if (bettransList.Any() && tpDailyList.Any())
            {
                var financeResult = bettransList.Select(bettrans
                    => bettrans.CheckData(FinanceCheckType.CompareBettransAndTpDailySummary, tpDailyList.FirstOrDefault(tp => tp.CurrencyId == bettrans.CurrencyId)));
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
            var tasks = new List<Task<IEnumerable<BettransCheckTransIdData>>>
            {
                _wagerClient.QueryAsync<BettransCheckTransIdData>("Finance_TpWagerRng_SG_Check_TransId_Get", Parameter),
                _wagerClient.QueryAsync<BettransCheckTransIdData>("Finance_TpWagerFish_SG_Check_TransId_Get", Parameter)
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
                        ProgressiveBet = bettrans.ProgressiveBet * exchangeRate,
                        ProgressiveWin = bettrans.ProgressiveWin * exchangeRate,
                    }).ToList();
        }
    }
}
