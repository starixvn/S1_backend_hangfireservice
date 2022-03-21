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

        public FinanceCheckResult Check(FinanceCheckType checkType, CheckData check)
        {
            var StakeDiff = Stake - check.Stake;
            var ValidBetDiff = ValidBet - check.ValidBet;
            var WinlostAmountDiff = WinlostAmount - check.WinlostAmount;
            
            var result = new StringBuilder();      
            if (StakeDiff > 0)
            {
                result.AppendLine($" －Stake difference amount is {StakeDiff}.");
            }
            if (ValidBetDiff > 0)
            {
                result.AppendLine($" －ValidBet difference amount is {ValidBetDiff}.");
            }
            if (WinlostAmountDiff > 0)
            {
                result.AppendLine($" －WinlostAmount difference amount is {WinlostAmountDiff}.");
            }

            return result.Length > 0 ? new FinanceCheckResult(this, result.ToString()) : null;
        }
    }
}
