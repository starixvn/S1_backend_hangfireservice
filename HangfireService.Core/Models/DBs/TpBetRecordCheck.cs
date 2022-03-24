using System.IO.Compression;
using System.Text;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Finance;
using Newtonsoft.Json;

namespace HangfireService.Core.Models.DBs
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

        public FinanceCheckResult Check(FinanceCheckType checkType, CheckData check)
        {
            var stakeDiff = Stake - check.Stake;
            var validBetDiff = ValidBet - check.ValidBet;
            var revenueDiff = Revenue - check.Revenue;
            var winlostAmountDiff = WinlostAmount - check.WinlostAmount;
            var progressiveBetDiff = ProgressiveBet - check.ProgressiveBet;
            var progressiveWinDiff = ProgressiveWin - check.ProgressiveWin;

            var result = new StringBuilder();      
            if (stakeDiff != 0)
            {
                result.AppendLine($" －Stake difference amount is {stakeDiff}.");
            }
            if (validBetDiff != 0)
            {
                result.AppendLine($" －ValidBet difference amount is {validBetDiff}.");
            }
            if (winlostAmountDiff != 0)
            {
                result.AppendLine($" －WinlostAmount difference amount is {winlostAmountDiff}.");
            }
            if (revenueDiff != 0)
            {
                result.AppendLine($" －Revenue difference amount is {revenueDiff}.");
            }
            if (progressiveBetDiff != 0)
            {
                result.AppendLine($" －ProgressiveBet difference amount is {progressiveBetDiff}.");
            }
            if (progressiveWinDiff != 0)
            {
                result.AppendLine($" －ProgressiveWin difference amount is {progressiveWinDiff}.");
            }

            return result.Length > 0 ? new FinanceCheckResult(this, result.ToString()) : null;
        }
    }
}
