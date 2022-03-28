using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Finance;
using HangfireService.Core.Models.Options;

namespace HangfireService.Core.ThirdParty.Interfaces
{
    public interface IFinanceTpHelper
    {
        int TpId { get; }

        Task<IEnumerable<BettransCheckData>> GetBettransCheckData(FinanceCheckOption checkOption);

        Task<IEnumerable<TpDailySummaryCheckData>> GetTpDailySummaryCheckData(FinanceCheckOption checkOption);

        /// <summary>
        /// 預設共用的檢查方法
        /// </summary>
        public static List<FinanceCheckResult> CheckData(FinanceCheckOption checkOption, IEnumerable<TpBetRecordCheck> tpBetRecord, IEnumerable<BettransCheckData> bettransList, IEnumerable<TpDailySummaryCheckData> tpDailyList)
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

            return checkResult;
        }
    }
}
