using System.Text;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Finance;
using HangfireService.Core.Models.Options;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

namespace HangfireService.Core.ThirdParty.Interfaces
{
    public interface IFinanceTpHelper
    {
        /// <summary>
        /// 是否詳細檢查TransId
        /// </summary>
        bool CheckTransId { get; }

        /// <summary>
        /// 綁定共用幣別
        /// 如主幣別3 子幣別72為共用幣別
        /// 主要依對方的帳(TpDailySummary)是否為公用幣別的主幣別
        /// </summary>
        bool BindCommonCurrency { get; }

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
                    => bettrans.CheckData(FinanceCheckType.CompareTpBetRecordSumAndBettrans, tpBetRecord.FirstOrDefault(tpBetRecord => tpBetRecord.CurrencyId == bettrans.CurrencyId)));
                checkResult.AddRange(bettransResult);
            }

            if (tpDailyList.Any())
            {
                var tpResult = tpDailyList.Select(tpDaily
                    => tpDaily.CheckData(FinanceCheckType.CompareTpDailySummaryAndTpBetRecordSum, tpBetRecord.FirstOrDefault(tpBetRecord => tpBetRecord.CurrencyId == tpDaily.CurrencyId)));
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
                _logger.LogError($"[{checkOption.BeginTime:yyyy-MM-dd}]:{JsonConvert.SerializeObject(data)}");
            }

            return checkResult;
        }

        public List<TpBetRecordCheck> ProcessTpBetRecordSubCurrencyToMainCurrency(List<TpBetRecordCheck> tpBetRecordData)
        {
            var mainCurrencyId = 3;
            var subCurrencyId = 72;
            bool hasMainCurrency = tpBetRecordData.Any(c => c.CurrencyId == mainCurrencyId);
            bool hasSubCurrency = tpBetRecordData.Any(c => c.CurrencyId == subCurrencyId);

            if (!hasMainCurrency && hasSubCurrency)
            {
                // 無主幣別資料將子幣別取代為主幣別
                ReplaceSubCurrencyWithMainCurrency(tpBetRecordData, mainCurrencyId, subCurrencyId);
            }
            else if (hasMainCurrency && hasSubCurrency)
            {
                // 加總
                AggregateCurrencyData(tpBetRecordData, mainCurrencyId, subCurrencyId);
            }
            return tpBetRecordData;
        }

        private void ReplaceSubCurrencyWithMainCurrency(List<TpBetRecordCheck> tpBetRecordData, int mainCurrencyId, int subCurrencyId)
        {
            TpBetRecordCheck subCurrencyRecord = tpBetRecordData.FirstOrDefault(i => i.CurrencyId == subCurrencyId);
            subCurrencyRecord.CurrencyId = mainCurrencyId;
        }

        private void AggregateCurrencyData(List<TpBetRecordCheck> tpBetRecordData, int mainCurrencyId, int subCurrencyId)
        {
            TpBetRecordCheck mainCurrencyRecord = tpBetRecordData.FirstOrDefault(i => i.CurrencyId == mainCurrencyId);
            TpBetRecordCheck subCurrencyRecord = tpBetRecordData.FirstOrDefault(i => i.CurrencyId == subCurrencyId);

            mainCurrencyRecord.Stake += subCurrencyRecord.Stake;
            mainCurrencyRecord.ValidBet += subCurrencyRecord.ValidBet;
            mainCurrencyRecord.WinlostAmount += subCurrencyRecord.WinlostAmount;
            mainCurrencyRecord.Revenue += subCurrencyRecord.Revenue;
            mainCurrencyRecord.ProgressiveBet += subCurrencyRecord.ProgressiveBet;
            mainCurrencyRecord.ProgressiveWin += subCurrencyRecord.ProgressiveWin;
        }

        public Task<IEnumerable<BettransCheckTransIdData>> GetBettransCheckTransIdData(FinanceCheckTransIdOption checkOption)
        {
            return null;
        }

        public List<FinanceCheckTransIdResult> CheckTransIdData(int tpId, FinanceCheckTransIdOption checkOption, List<TpBetRecordCheckTransId> tpBetRecord, List<BettransCheckTransIdData> bettransList)
        {
            var checkResult = new List<FinanceCheckTransIdResult>();
            var bettransOfTransIds = new List<string>();
            var err = new List<string>();
            var res = new List<string>();

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
                checkResult.Add(new FinanceCheckTransIdResult(FinanceCheckTransIdType.CompareBettransListAndTpBetRecordList, TpId, res.Count, res));
                return checkResult;
            }
            else if (bettransList.Any() && !tpBetRecord.Any())
            {
                bettransOfTransIds = bettransList.Select(x => x.TransId).ToList();
                checkResult.Add(new FinanceCheckTransIdResult(FinanceCheckTransIdType.CompareEachTransIdBettransAndTpBetRecord, TpId, bettransOfTransIds.Count, bettransOfTransIds));
                return checkResult;
            }

            if (err.Any())
            {
                checkResult.Add(new FinanceCheckTransIdResult(FinanceCheckTransIdType.CompareBettransAndTpBetRecordDataAbsence, TpId, err.Count, err));
                return checkResult;
            }

            bettransOfTransIds = bettransList.Select(x => x.TransId).ToList();
            var tpBetRecordOfTransIds = tpBetRecord.Select(x => x.TransId).ToList();

            var checkBetTransId = bettransOfTransIds.Except(tpBetRecordOfTransIds).ToList();
            var checkTpBetTransId = tpBetRecordOfTransIds.Except(bettransOfTransIds).ToList();
            if (checkBetTransId.Any())
            {
                err.Clear();
                err.Add("transid bettrans not in tpbetrecord.");
                checkResult.Add(new FinanceCheckTransIdResult(
                    FinanceCheckTransIdType.CompareEachTransIdBettransAndTpBetRecord, 
                    TpId, 
                    checkBetTransId.Count, 
                    checkBetTransId, 
                    JsonConvert.SerializeObject(err)));
            }

            if (checkTpBetTransId.Any())
            {
                err.Clear();
                err.Add("transid tpbetrecord not in bettrans.");
                checkResult.Add(new FinanceCheckTransIdResult(FinanceCheckTransIdType.CompareTransIdTpBetRecordAndBettrans, TpId, checkTpBetTransId.Count, checkTpBetTransId, JsonConvert.SerializeObject(err)));
            }

            var tpResult = bettransList.Select(bettrans
                => bettrans.CheckTransIdData(tpId, FinanceCheckTransIdType.CompareBettransListAndTpBetRecordList, tpBetRecord.FirstOrDefault(tpBetRecord => tpBetRecord.TransId == bettrans.TransId)));
            var tpData = tpResult.Where(item => item != null).ToList();

            if (tpData.Any())
            {
                var errList = new List<string>
                {
                    JsonConvert.SerializeObject(tpData)
                };
                checkResult.Add(new FinanceCheckTransIdResult(FinanceCheckTransIdType.CompareBettransListAndTpBetRecordList, TpId, tpData.Count, errList, FormatErrorData(tpData)));
            }

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
            else
            {
                checkResult.Add(new FinanceCheckTransIdResult(FinanceCheckTransIdType.CompareBettransListAndTpBetRecordList, TpId, res.Count, res));
            }

            return checkResult;
        }
        private static string FormatErrorData(List<ErrorDetail> errorDetails) {
            var result = new StringBuilder();
            foreach (var erorrDetail in errorDetails) {
                result.AppendLine(erorrDetail.GetDiffMessage());
            }
            return result.ToString();
        }

    }
}
