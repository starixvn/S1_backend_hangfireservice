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
    public class MGTpHelper : BaseTpHelper, IFinanceTpHelper
    {
        public bool CheckTransId => true;

        public TimeOption TimeOptionId => TimeOption.SettledTime;

        public int TpId { get; } = 34;

        private readonly ReportDBDataLayer _reportDbDataLayer;
        private readonly IEnumerable<TpCurrency> _tpCurrency;
        private readonly MySqlClient _dbClient;
        private readonly PartitionService _partitionService;
        private readonly FincanceService _fincanceService;

        public MGTpHelper(IServiceProvider provider) : base(provider)
        {
            _reportDbDataLayer = provider.GetRequiredService<ReportDBDataLayer>();

            _tpCurrency = _reportDbDataLayer.GetTpCurrency(TpId);

            _dbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);

            _partitionService = provider.GetRequiredService<PartitionService>();

            _fincanceService = provider.GetRequiredService<FincanceService>();
        }

        public async Task<IEnumerable<BettransCheckData>> GetBettransCheckData(FinanceCheckOption checkOption)
        {
            var BeginTime = checkOption.BeginTime.Ticks;
            var EndTime = checkOption.EndTime.Ticks;
            var tasks = new List<Task<IEnumerable<BettransCheckData>>>
            {
                //Slot
                _dbClient.QueryAsync<BettransCheckData>("Finance_BettransRng_MG_Summary_Get_V3", new { _BeginTime = BeginTime, _EndTime = EndTime }, 120)
            };
            //Live Casino
            tasks.AddRange(_partitionService.QueryAsync<BettransCheckData>(checkOption.BeginTime, checkOption.EndTime, "Finance_BettransLC_MG_Summary_Get_V2", new { _BeginTime = BeginTime, _EndTime = EndTime }));

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
            //排除MG活動金額
            var freeGameReward = _fincanceService.GetTpDailySummary((int)SummaryType.TpRewardFreeGame, TpId, checkOption.BeginTime, checkOption.EndTime).Result;
            var totalReward = _fincanceService.GetTpDailySummary((int)SummaryType.TpRewardTotalAmount, TpId, checkOption.BeginTime, checkOption.EndTime).Result;
            var cashReward = _fincanceService.GetTpDailySummary((int)SummaryType.TpRewardCash, TpId, checkOption.BeginTime, checkOption.EndTime).Result;

            foreach (var tpDaily in tpDailyList)
            {
                var freeGame = freeGameReward.FirstOrDefault(i => i.CurrencyId == tpDaily.CurrencyId)?.Reward ?? 0m;
                tpDaily.Stake -= freeGame;
                tpDaily.ValidBet -= freeGame;
                tpDaily.WinlostAmount = (tpDaily.WinlostAmount * -1) + (totalReward.FirstOrDefault(i => i.CurrencyId == tpDaily.CurrencyId)?.Reward ?? 0m);
            }

            foreach (var bettrans in bettransList)
            {
                var cash = cashReward.FirstOrDefault(i => i.CurrencyId == bettrans.CurrencyId)?.Reward ?? 0m;
                bettrans.Payout -= cash;
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
                    totalReward,
                    freeGameReward,
                    cashReward
                };
                _logger.LogError($"[{checkOption.BeginTime:yyyy-MM-dd}]:{ToJson(data)}");
            }
            return checkResult;
        }

        public async Task<IEnumerable<BettransCheckTransIdData>> GetBettransCheckTransIdData(FinanceCheckTransIdOption checkOption)
        {
            var BeginTime = checkOption.BeginTime.GetUnixTime().GetTicksFormUnixTime();
            var EndTime = checkOption.EndTime.GetUnixTime().GetTicksFormUnixTime();

            var tasks = new List<Task<IEnumerable<BettransCheckTransIdData>>>
            {
                _dbClient.QueryAsync<BettransCheckTransIdData>("Finance_BettransRng_MG_Check_TransId_Get", new { _BeginTime = BeginTime, _EndTime = EndTime }),
            };

            tasks.AddRange(_partitionService.QueryAsync<BettransCheckTransIdData>(checkOption.BeginTime, checkOption.EndTime, "Finance_BettransLC_MG_Check_TransId_Get", new { _BeginTime = BeginTime, _EndTime = EndTime }));

            await Task.WhenAll(tasks);

            var CheckData = new List<BettransCheckTransIdData>();
            foreach (var item in tasks)
            {
                CheckData.AddRange(item.Result);
            }

            return (from bettrans in CheckData
                    select new BettransCheckTransIdData
                    {
                        TpId = TpId,
                        TransId = bettrans.TransId,
                        CurrencyId = bettrans.CurrencyId,
                        Stake = bettrans.Stake,
                        ValidBet = bettrans.ValidBet,
                        Payout = 0,
                        WinlostAmount = bettrans.WinlostAmount,
                        Revenue = 0,
                        ProgressiveBet = bettrans.ProgressiveBet,
                        ProgressiveWin = bettrans.ProgressiveWin,
                    }).ToList();
        }
    }
}
