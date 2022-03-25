using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using System.Text;

namespace HangfireService.Core.Models.Finance
{
    public class TpBetRecordCheck
    {
        public int TpId { get; set; }

        public int CurrencyId { get; set; }

        public decimal Stake { get; set; }

        public decimal ValidBet { get; set; }

        public decimal WinlostAmount { get; set; }

        public decimal Revenue { get; set; }

        public decimal ProgressiveBet { get; set; }

        public decimal ProgressiveWin { get; set; }

        public FinanceCheckResult CheckData(FinanceCheckType checkType, TpBetRecordCheck? check)
        {
            var result = new List<string>();
            if (check == null)
            {
                result.Add($"TpBetRecord not found CurrencyId:{CurrencyId} data.");
                return  new FinanceCheckResult(checkType, CurrencyId, result);
            }
            var stakeDiff = Stake - check.Stake;
            var validBetDiff = ValidBet - check.ValidBet;
            var revenueDiff = Revenue - check.Revenue;
            var winlostAmountDiff = Math.Abs(WinlostAmount) - Math.Abs(check.WinlostAmount);
            var progressiveBetDiff = ProgressiveBet - check.ProgressiveBet;
            var progressiveWinDiff = ProgressiveWin - check.ProgressiveWin;

            
            if (stakeDiff != 0)
            {
                result.Add($"Stake difference amount is {stakeDiff}.");
            }
            if (validBetDiff != 0)
            {
                result.Add($"ValidBet difference amount is {validBetDiff}.");
            }
            if (winlostAmountDiff != 0)
            {
                result.Add($"WinlostAmount difference amount is {winlostAmountDiff}.");
            }
            if (revenueDiff != 0)
            {
                result.Add($"Revenue difference amount is {revenueDiff}.");
            }
            if (progressiveBetDiff != 0)
            {
                result.Add($"ProgressiveBet difference amount is {progressiveBetDiff}.");
            }
            if (progressiveWinDiff != 0)
            {
                result.Add($"ProgressiveWin difference amount is {progressiveWinDiff}.");
            }

            return new FinanceCheckResult(checkType, CurrencyId, result);
        }
    }

    public class BettransCheckData : TpBetRecordCheck
    {
        public string TpCurrencyId { get; set; }

        public decimal Payout { get; set; }

        public FinanceCheckResult CheckData(FinanceCheckType checkType, TpDailySummaryCheckData? check)
        {
            var result = new List<string>();
            if (check == null)
            {
                result.Add($"TpDailySummary not found CurrencyId:{CurrencyId} data.");
                return new FinanceCheckResult(checkType, CurrencyId, result);
            }

            var baseCheck = base.CheckData(checkType, check);
            var payoutDiff = Payout - check.Payout;
            if (payoutDiff != 0)
            {
                result.Add($"Payout difference amount is {payoutDiff}.");
                if (baseCheck != null)
                {
                    baseCheck.ErrorList.AddRange(result);
                }
                else
                {
                    baseCheck = new FinanceCheckResult(checkType, CurrencyId, result);
                }
            }

            return baseCheck;
        }
    }

    public class TpDailySummaryCheckData : TpBetRecordCheck
    {
        public decimal Payout { get; set; }
    }
}
