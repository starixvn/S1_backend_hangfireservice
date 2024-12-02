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
    internal class PTTpHelper : BaseTpHelper, IFinanceTpHelper
    {
        public bool CheckTransId => false;
        public bool BindCommonCurrency => true;
        public TimeOption TimeOptionId => TimeOption.SettledTime;

        public int TpId => 27;
        private readonly ReportDBDataLayer _reportDbDataLayer;
        private readonly IEnumerable<TpCurrency> _tpCurrency;
        private readonly MySqlClient _wagerClient;
        private readonly FincanceService _fincanceService;
        private readonly Dictionary<int, string> _currencyTimeZoneList;

        public PTTpHelper(IServiceProvider provider) : base(provider)
        {
            _reportDbDataLayer = provider.GetRequiredService<ReportDBDataLayer>();

            _tpCurrency = _reportDbDataLayer.GetTpCurrency(TpId);

            _wagerClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);

            _fincanceService = provider.GetRequiredService<FincanceService>();

            _currencyTimeZoneList = provider.GetRequiredService<IEnumerable<CurrencyOption>>().ToDictionary(d => d.CurrencyId, d => d.TimeZone);
            //修正PT IDR時區
            _currencyTimeZoneList.Remove(15);
            _currencyTimeZoneList.Add(15, "+08:00");
            //因INR報表時間為 00:30:00 ~ 00:30:00 時區增加半小時(原+05:30)
            _currencyTimeZoneList.Remove(61);
            _currencyTimeZoneList.Add(61, "+05:00");
        }

        public async Task<IEnumerable<BettransCheckData>> GetBettransCheckData(FinanceCheckOption checkOption)
        {
            var BeginTime = checkOption.BeginTime;
            var EndTime = checkOption.EndTime.AddSeconds(-1);
            var tasks = new List<Task<IEnumerable<BettransCheckData>>>();

            foreach (var item in _tpCurrency)
            {
                if (!_currencyTimeZoneList.ContainsKey(item.CurrencyId) || item.CurrencyId == 72)
                {
                    continue;
                }

                // 依各幣別轉換時間
                var timeZone = _currencyTimeZoneList[item.CurrencyId];
                var Begin = BeginTime.ToOffset(timeZone).ToUnixTimeSeconds();
                var End = EndTime.ToOffset(timeZone).ToUnixTimeSeconds() - 1; // 測試不含最後一秒
                tasks.Add(_wagerClient.QueryAsync<BettransCheckData>("Finance_TpWagerRng_PT_Summary_Get", new { _BeginTime = Begin, _EndTime = End, _TpCurrencyId = item.TpCurrencyId }));
                tasks.Add(_wagerClient.QueryAsync<BettransCheckData>("Finance_TpWagerLC_PT_Summary_Get", new { _BeginTime = Begin, _EndTime = End, _TpCurrencyId = item.TpCurrencyId }));
            }

            await Task.WhenAll(tasks);

            var CheckData = new List<BettransCheckData>();
            foreach (var item in tasks)
            {
                CheckData.AddRange(item.Result);
            }

            //使用TpCurrencyId組合Slot跟Live Casino資料
            return CheckData
                .GroupBy(i => i.TpCurrencyId)
                .Select(i => new BettransCheckData
                {
                    TpId = TpId,
                    TpCurrencyId = i.Key,
                    CurrencyId = _tpCurrency.First(c => c.TpCurrencyId == i.Key).CurrencyId,
                    Stake = i.Sum(i => i.Stake),
                    ValidBet = i.Sum(i => i.ValidBet),
                    Payout = i.Sum(i => i.Payout),
                    WinlostAmount = i.Sum(i => i.WinlostAmount),
                    Revenue = 0,
                    ProgressiveBet = i.Sum(i => i.ProgressiveBet),
                    ProgressiveWin = i.Sum(i => i.ProgressiveWin),
                });
        }

        public async Task<IEnumerable<TpDailySummaryCheckData>> GetTpDailySummaryCheckData(FinanceCheckOption checkOption)
        {
            // 各幣別0點整日報表 2022-08-01 00:00:00~20220-08-02 00:00:00，INR為 00:30:00 ~ 00:30:00
            var tpDailyList = await _fincanceService.GetTpDailySummary((int)SummaryType.TpBetRecord, TpId, checkOption.BeginTime, checkOption.EndTime);
            foreach (var tpDaily in tpDailyList)
            {
                var rate = _tpCurrency.First(c => c.CurrencyId == tpDaily.CurrencyId).ExchangeRate;
                tpDaily.Stake *= rate;
                tpDaily.ValidBet *= rate;
                tpDaily.WinlostAmount = (tpDaily.WinlostAmount * -1) * rate;
                tpDaily.ProgressiveBet *= rate;
                tpDaily.ProgressiveWin *= rate;
                tpDaily.Payout *= rate;
            }
            return tpDailyList;
        }

        public List<FinanceCheckResult> CheckData(FinanceCheckOption checkOption, List<TpBetRecordCheck> tpBetRecord, List<BettransCheckData> bettransList, List<TpDailySummaryCheckData> tpDailyList)
        {
            // 取得各幣別時間資料
            foreach (var currencyId in _tpCurrency.Select(i => i.CurrencyId))
            {
                var timeZone = _currencyTimeZoneList[currencyId];
                var Begin = DateTimeOffset.FromUnixTimeSeconds(checkOption.BeginTime.ToOffset(timeZone).ToUnixTimeSeconds()).DateTime;
                var End = DateTimeOffset.FromUnixTimeSeconds(checkOption.EndTime.ToOffset(timeZone).ToUnixTimeSeconds()).DateTime;
                // 取代原始資料
                tpBetRecord.RemoveAll(s => s.CurrencyId == currencyId);
                tpBetRecord.AddRange(_reportDbDataLayer.GetTpBetRecordCheckCurrencyIdByTpId(TpId, currencyId, Begin, End).ToList());
            }
            tpBetRecord = ProcessTpBetRecordSubCurrencyToMainCurrency(tpBetRecord);

            // 調整為tpbetrecord計算公式
            foreach (var tpDaily in tpDailyList)
            {
                tpDaily.Stake += tpDaily.ProgressiveBet;
                tpDaily.ValidBet += tpDaily.ProgressiveBet;
                tpDaily.WinlostAmount = (tpDaily.WinlostAmount * -1) - tpDaily.ProgressiveBet;
            }

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
    }
}
