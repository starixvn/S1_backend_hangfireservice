using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Finance;
using HangfireService.Core.Models.Options;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using System.Linq;

namespace HangfireService.Core.ThirdParty.Interfaces
{
    public interface IFinanceTpHelper
    {
        /// <summary>
        /// 是否詳細檢查TransId
        /// </summary>
        bool CheckTransId { get; }

        /// <summary>
        /// TpBetRecord使用時間欄位
        /// </summary>
        TimeOption TimeOptionId { get; }

        int TpId { get; }

        ILogger<BaseTpHelper> _logger { get; set; }

        Task<IEnumerable<BettransCheckData>> GetBettransCheckData(FinanceCheckOption checkOption);

        Task<IEnumerable<TpDailySummaryCheckData>> GetTpDailySummaryCheckData(FinanceCheckOption checkOption);

        /// <summary>
        /// 預設共用的檢查方法
        /// </summary>
        public List<FinanceCheckResult> CheckData(FinanceCheckOption checkOption, List<TpBetRecordCheck> tpBetRecord, List<BettransCheckData> bettransList, List<TpDailySummaryCheckData> tpDailyList)
        {
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
                };
                _logger.LogError($"[{checkOption.BeginTime:yyyy-MM-dd}]:{JsonConvert.SerializeObject(data)}");
            }

            return checkResult;
        }

        public Task<IEnumerable<BettransCheckTransIdData>> GetBettransCheckTransIdData(FinanceCheckTransIdOption checkOption)
        {
            return null;
        }

        public List<FinanceCheckTransIdResult> CheckTransIdData(FinanceCheckTransIdOption checkOption, List<TpBetRecordCheckTransId> tpBetRecord, List<BettransCheckTransIdData> bettransList)
        {
            var checkResult = new List<FinanceCheckTransIdResult>();

            var err = new List<string>();

            // 資料檢查
            if (!bettransList.Any())
            {
                err.Add($"bettransList no data.");
            }

            if (!tpBetRecord.Any())
            {
                err.Add($"tpBetRecord no data.");
            }

            if (!bettransList.Any() && !tpBetRecord.Any())
            {
                // 無資料無須檢查
                _logger.LogInformation($"[{checkOption.BeginTime:yyyy-MM-dd HH}] TpId:{TpId} 帳務單號檢查無資料");
                return checkResult;
            }

            if (err.Any())
            {
                checkResult.Add(new FinanceCheckTransIdResult(FinanceCheckTransIdType.結算與交易資料檢查, TpId, err.Count, err));
                return checkResult;
            }

            // 單號檢查
            var bettransOfTransIds = bettransList.Select(x => x.TransId).ToList();
            var tpBetRecordOfTransIds = tpBetRecord.Select(x => x.TransId).ToList();

            var checkBetTransId = bettransOfTransIds.Except(tpBetRecordOfTransIds).ToList();
            var checkTpBetTransId = tpBetRecordOfTransIds.Except(bettransOfTransIds).ToList();
            if (checkBetTransId.Any())
            {
                checkResult.Add(new FinanceCheckTransIdResult(FinanceCheckTransIdType.交易與結算單號檢查, TpId, checkBetTransId.Count, checkBetTransId));
            }

            if (checkTpBetTransId.Any())
            {
                checkResult.Add(new FinanceCheckTransIdResult(FinanceCheckTransIdType.結算與交易單號檢查, TpId, checkTpBetTransId.Count, checkTpBetTransId));
            }

            // 金額檢查
            var tpResult = bettransList.Select(bettrans
                => bettrans.CheckTransIdData(FinanceCheckTransIdType.結算與交易金額檢查, tpBetRecord.FirstOrDefault(tpBetRecord => tpBetRecord.TransId == bettrans.TransId)));
            var tpData = tpResult.Where(item => item != null).Select(item => item.ErrorList).ToList();

            if (tpData.Any())
            {
                checkResult.Add(new FinanceCheckTransIdResult(FinanceCheckTransIdType.結算與交易金額檢查, TpId, tpData.Count, null, JsonConvert.SerializeObject(tpData)));
            }

            // 原資料
            if (checkResult.Any())
            {
                var data = new
                {
                    TpId,
                    tpBetRecord,
                    bettransList,
                };
                _logger.LogError($"[{checkOption.BeginTime:yyyy-MM-dd HH}]:{JsonConvert.SerializeObject(data)}");
            }

            return checkResult;
        }
    }
}
