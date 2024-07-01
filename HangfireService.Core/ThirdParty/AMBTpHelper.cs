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
    internal class AMBTpHelper : BaseTpHelper, IFinanceTpHelper
    {
        public bool CheckTransId => false;
        public bool BindCommonCurrency => true;

        public TimeOption TimeOptionId => TimeOption.SettledTime;

        public int TpId => 30;
        private readonly ReportDBDataLayer _reportDbDataLayer;
        private readonly IEnumerable<TpCurrency> _tpCurrency;
        private readonly MySqlClient _dbClient;
        private readonly FincanceService _fincanceService;
        private readonly PartitionService _partitionService;
        // 對方報表時間為2022-07-26+8 ~ 2022-07-26+8 ，UTC+0轉換為2022-07-25 16:00:00
        private readonly int utcTime = -8;

        public AMBTpHelper(IServiceProvider provider) : base(provider)
        {
            _reportDbDataLayer = provider.GetRequiredService<ReportDBDataLayer>();

            _tpCurrency = _reportDbDataLayer.GetTpCurrency(TpId);

            _dbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);

            _partitionService = provider.GetRequiredService<PartitionService>();

            _fincanceService = provider.GetRequiredService<FincanceService>();
        }

        public async Task<IEnumerable<BettransCheckData>> GetBettransCheckData(FinanceCheckOption checkOption)
        {
            var BeginTime = (long)checkOption.BeginTime.AddHours(utcTime).Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            var EndTime = (long)checkOption.EndTime.AddHours(utcTime).Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            var tasks = new List<Task<IEnumerable<BettransCheckData>>>
            {
                // Poker
                _dbClient.QueryAsync<BettransCheckData>("Finance_BettransPK_AMB_Summary_Get", new { _BeginTime = BeginTime, _EndTime = EndTime })
            };

            //Slot
            tasks.AddRange(_partitionService.QueryAsync<BettransCheckData>(checkOption.BeginTime.AddHours(utcTime), checkOption.EndTime.AddHours(utcTime), "Finance_BettransRng_AMB_Summary_Get", new { _BeginTime = BeginTime, _EndTime = EndTime }));
            await Task.WhenAll(tasks);

            var CheckData = new List<BettransCheckData>();
            foreach (var item in tasks)
            {
                var data = item.Result;
                data = ProcessBettransSubCurrencyToMainCurrency(data);
                CheckData.AddRange(data);
            }

            //使用TpCurrencyId組合Slot跟Poker資料
            return CheckData
                .GroupBy(i => i.TpCurrencyId)
                .Select(i => new BettransCheckData
                {
                    TpId = TpId,
                    TpCurrencyId = i.Key,
                    CurrencyId = _tpCurrency.First(c => c.TpCurrencyId == i.Key).CurrencyId,
                    Stake = i.Sum(i => i.Stake),
                    ValidBet = i.Sum(i => i.ValidBet),
                    Payout = 0,
                    WinlostAmount = i.Sum(i => i.WinlostAmount),
                    Revenue = i.Sum(i => i.Revenue),
                    ProgressiveBet = 0,
                    ProgressiveWin = 0,
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
                tpDaily.WinlostAmount *= rate;
                tpDaily.Payout = 0;
                tpDaily.Revenue *= rate;
            }
            return tpDailyList;
        }

        public List<FinanceCheckResult> CheckData(FinanceCheckOption checkOption, List<TpBetRecordCheck> tpBetRecord, List<BettransCheckData> bettransList, List<TpDailySummaryCheckData> tpDailyList)
        {
            tpBetRecord = _reportDbDataLayer.GetTpBetRecordCheckDataByTpId(TpId, checkOption.BeginTime.AddHours(utcTime), checkOption.EndTime.AddHours(utcTime), TicketStatusExcludeOption.Cancel, TimeOption.SettledTime).ToList();
            tpBetRecord = ProcessTpBetRecordSubCurrencyToMainCurrency(tpBetRecord);

            // bettrans ValidBet 設為 結算資料 ValidBet，因bettrans對方沒有提供ValidBet
            foreach (var bettransData in bettransList)
            {
                bettransData.ValidBet = tpBetRecord.First(x => x.CurrencyId == bettransData.CurrencyId).ValidBet;
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
