using System.Text;
using HangfireService.Core.Models.Enums;

namespace HangfireService.Core.Models.Finance
{
    public class FinanceCheckResult
    {
        public FinanceCheckType FinanceCheckType { get; set; }

        public int CurrencyId { get; set; }

        public List<string> ErrorList { get; set; }

        public FinanceCheckResult(FinanceCheckType financeCheckType, int currencyId, List<string> errorList)
        {
            FinanceCheckType = financeCheckType;
            CurrencyId = currencyId;
            ErrorList = errorList;
        }
    }

    public class FinanceCheckTransIdResult
    {
        public FinanceCheckTransIdType FinanceCheckTransIdType { get; set; }

        public int TpId { get; set; }

        public int Count { get; set; }

        public List<string> ErrorList { get; set; }
        public string ErrorData { get; set; }

        public FinanceCheckTransIdResult(FinanceCheckTransIdType financeCheckTransIdType, int tpId, int count, List<string> errorList, string errorData = null)
        {
            FinanceCheckTransIdType = financeCheckTransIdType;
            TpId = tpId;
            Count = count;
            ErrorList = errorList;
            ErrorData = errorData;
        }
    }

    public class FinanceCheckTransIdData
    {
        public long SQ { get; set; }

        public int TpId { get; set; }

        public string TransId { get; set; }

        public int FinanceCheckTransIdType { get; set; }

        public DateTime CheckTime { get; set; }

        public string ErrorData { get; set; }

        public DateTime CreateTime { get; set; }
    }

    public class ErrorDetail
    {
        public string TransId { get; set; }

        public decimal Stake { get; set; }

        public decimal ValidBet { get; set; }

        public decimal WinlostAmount { get; set; }

        public decimal Revenue { get; set; }

        public decimal ProgressiveBet { get; set; }

        public decimal ProgressiveWin { get; set; }

        public string GetDiffMessage() {
            var msg = new StringBuilder();
            msg.AppendLine($"- Trans Id : {TransId}:");
            
            if (IsDiffNoticable(Stake)) {
                msg.AppendLine($" Stake Diff: {Stake:f4}");
            }
            if (IsDiffNoticable(ValidBet)) {
                msg.AppendLine($" ValidBet Diff: {ValidBet:f4}");
            }
            if (IsDiffNoticable(WinlostAmount)) {
                msg.AppendLine($" WinLostAmount Diff: {WinlostAmount:f4}");
            }

            return msg.ToString();
        }

        private static bool IsDiffNoticable(decimal diff) {
            return diff != 0 && Math.Abs(diff) > 0.01m;
        }
    }
}
