using HangfireService.Core.DataLayers;
using HangfireService.Core.Extensions;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Finance;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Service;
using HangfireService.Core.ThirdParty.Interfaces;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace HangfireService.Core.ThirdParty
{
    internal class GPITpHelper : BaseTpHelper, IFinanceTpHelper
    {
        public bool CheckTransId => true;
        public bool BindCommonCurrency => true;

        public TimeOption TimeOptionId => TimeOption.TransTime;

        public int TpId => 6;

        private readonly ReportDBDataLayer _reportDbDataLayer;
        private readonly IEnumerable<TpCurrency> _tpCurrency;
        private readonly MySqlClient _wagerClient;
        private readonly FincanceService _fincanceService;

        public GPITpHelper(IServiceProvider provider) : base(provider)
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

            var tasks = new List<Task<IEnumerable<BettransCheckData>>>
            {
                _wagerClient.QueryAsync<BettransCheckData>("Finance_TpWagerLT_GPI_Summary_Get", new { _BeginTime = BeginTime, _EndTime = EndTime }),
                //_wagerClient.QueryAsync<BettransCheckData>("Finance_TpWagerRng_GPI_Summary_Get", new { _BeginTime = BeginTime, _EndTime = EndTime })
            };

            await Task.WhenAll(tasks);

            var CheckData = new List<BettransCheckData>();
            foreach (var item in tasks)
            {
                var data = item.Result;
                data = ProcessBettransSubCurrencyToMainCurrency(data);
                CheckData.AddRange(data);
            }

            //使用CurrencyId組合資料
            var bettransData = CheckData
                .GroupBy(i => i.TpCurrencyId)
                .Select(i => new BettransCheckData
                {
                    TpId = TpId,
                    TpCurrencyId = i.Key,
                    CurrencyId = _tpCurrency.First(c => c.TpCurrencyId == i.Key).CurrencyId,
                    Stake = i.Sum(i => i.Stake) * _tpCurrency.First(c => c.TpCurrencyId == i.Key).ExchangeRate,
                    ValidBet = i.Sum(i => i.ValidBet) * _tpCurrency.First(c => c.TpCurrencyId == i.Key).ExchangeRate,
                    Payout = 0,
                    WinlostAmount = i.Sum(i => i.WinlostAmount) * _tpCurrency.First(c => c.TpCurrencyId == i.Key).ExchangeRate,
                    Revenue = 0,
                    ProgressiveBet = i.Sum(i => i.ProgressiveBet) * _tpCurrency.First(c => c.TpCurrencyId == i.Key).ExchangeRate,
                    ProgressiveWin = i.Sum(i => i.ProgressiveWin) * _tpCurrency.First(c => c.TpCurrencyId == i.Key).ExchangeRate,
                });

            return bettransData;
        }

        public async Task<IEnumerable<TpDailySummaryCheckData>> GetTpDailySummaryCheckData(FinanceCheckOption checkOption)
        {
            // GPI無報表API 資料為Wager總計
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
            // 暫時僅檢查Lottery
            tpBetRecord = _reportDbDataLayer.GetTpBetRecordCheckDataByTpId(TpId, checkOption.BeginTime, checkOption.EndTime, TicketStatusExcludeOption.CancelAndUnsettle, TimeOption.SettledTime).ToList();
            tpBetRecord = ProcessTpBetRecordSubCurrencyToMainCurrency(tpBetRecord);

            // 排除Slot計算
            List<TpBetRecordCheck> tpBetRecordSlot = _reportDbDataLayer.GetTpBetRecordCheckByGameType(TpId, 11, checkOption.BeginTime, checkOption.EndTime).ToList();
            if (tpBetRecordSlot.Count > 0)
            {
                foreach (var tpBetData in tpBetRecord.ToList())
                {
                    var tpBetItem = tpBetRecordSlot.Where(x => x.CurrencyId == tpBetData.CurrencyId).FirstOrDefault();
                    if (tpBetItem == null)
                    {
                        continue;
                    }
                    tpBetData.Stake -= tpBetItem.Stake;
                    tpBetData.ValidBet -= tpBetItem.ValidBet;
                    tpBetData.WinlostAmount -= tpBetItem.WinlostAmount;
                    tpBetData.ProgressiveBet -= tpBetItem.ProgressiveBet;
                    tpBetData.ProgressiveWin -= tpBetItem.ProgressiveWin;
                    if (tpBetData.Stake == 0m)
                    {
                        tpBetRecord.Remove(tpBetData);
                    }
                }
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
            var EndTime = checkOption.EndTime.GetUnixTime() - 1;

            var tasks = new List<Task<IEnumerable<BettransCheckTransIdData>>>
            {
                _wagerClient.QueryAsync<BettransCheckTransIdData>("Finance_TpWagerLT_GPI_Check_TransId_Get", new { _BeginTime = BeginTime, _EndTime = EndTime }),
                _wagerClient.QueryAsync<BettransCheckTransIdData>("Finance_TpWagerRng_GPI_Check_TransId_Get", new { _BeginTime = BeginTime, _EndTime = EndTime })
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
