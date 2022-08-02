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
    internal class PTTpHelper : BaseTpHelper, IFinanceTpHelper
    {
        public int TpId => 27;
        private readonly ReportDBDataLayer _reportDbDataLayer;
        private readonly IEnumerable<TpCurrency> _tpCurrency;
        private readonly MySqlClient _wagerClient;
        private readonly FincanceService _fincanceService;
        private readonly Dictionary<string, string> _currencyTimeZoneList = new Dictionary<string, string>() {
            {"MYR", "08:00"},
            {"USD", "00:00"},
            {"THB", "07:00"},
            {"CNY", "08:00"},
            {"IDR", "08:00"},
            {"INR", "05:30"},
            {"VND", "07:00"}
        };

        public PTTpHelper(IServiceProvider provider) : base(provider)
        {
            _reportDbDataLayer = provider.GetRequiredService<ReportDBDataLayer>();

            _tpCurrency = _reportDbDataLayer.GetTpCurrency(TpId);

            _wagerClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);

            _fincanceService = provider.GetRequiredService<FincanceService>();
        }

        public async Task<IEnumerable<BettransCheckData>> GetBettransCheckData(FinanceCheckOption checkOption)
        {
            var BeginTime = checkOption.BeginTime;
            var EndTime = checkOption.EndTime;
            var tasks = new List<Task<IEnumerable<BettransCheckData>>>();

            foreach (var item in _tpCurrency)
            {
                // 依各幣別轉換時間
                var Begin = GetReportTimeByCurrency(BeginTime, item.TpCurrencyId);
                var End = GetReportTimeByCurrency(EndTime, item.TpCurrencyId);

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
            foreach (var item in _tpCurrency)
            {
                var Begin = DateTimeOffset.FromUnixTimeSeconds(GetReportTimeByCurrency(checkOption.BeginTime, item.TpCurrencyId)).DateTime;
                var End = DateTimeOffset.FromUnixTimeSeconds(GetReportTimeByCurrency(checkOption.EndTime, item.TpCurrencyId)).DateTime;
                // 取代原始資料
                tpBetRecord.RemoveAll(s => s.CurrencyId == item.CurrencyId);
                tpBetRecord.AddRange(_reportDbDataLayer.GetTpBetRecordCheckCurrencyIdByTpId(TpId, item.CurrencyId, Begin, End).ToList());
            }

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

        private long GetReportTimeByCurrency(DateTime time, string currency)
        {
            var getCurrencyTimeZone = _currencyTimeZoneList[currency];
            string[] CurrencyTimeZoneList = getCurrencyTimeZone.Split(":");
            var timezoneHour = Convert.ToInt32(CurrencyTimeZoneList[0]);
            var timezoneMinute = Convert.ToInt32(CurrencyTimeZoneList[1]);

            return new DateTimeOffset(new DateTimeOffset(time, TimeSpan.Zero).DateTime, new TimeSpan(timezoneHour, timezoneMinute, 0)).ToUnixTimeSeconds();
        }
    }
}
