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
                result.Add($"Stake 差額:{stakeDiff:f4}");
            }
            if (validBetDiff != 0)
            {
                result.Add($"ValidBet 差額:{validBetDiff:f4}.");
            }
            if (winlostAmountDiff != 0)
            {
                result.Add($"WinlostAmount 差額:{winlostAmountDiff}.");
            }
            if (revenueDiff != 0)
            {
                result.Add($"Revenue 差額:{revenueDiff:f4}.");
            }
            if (progressiveBetDiff != 0)
            {
                result.Add($"ProgressiveBet 差額:{progressiveBetDiff:f4}.");
            }
            if (progressiveWinDiff != 0)
            {
                result.Add($"ProgressiveWin 差額:{progressiveWinDiff:f4}.");
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
                result.Add($"Payout 差額:{payoutDiff:f4}.");
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

        public decimal Reward { get; set; }
    }
}
