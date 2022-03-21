using HangfireService.Core.Models.DBs;

namespace HangfireService.Core.Models.Finance
{
    public class FinanceCheckResult
    {

        public int TpId { get; set; }

        public int CurrencyId { get; set; }

        public string ErrorMessage { get; set; }

        public FinanceCheckResult(int tpId, string errorMessage)
        {
            TpId = tpId;
            ErrorMessage = errorMessage;
        }

        public FinanceCheckResult(TpBetRecordCheck tpBetRecordCheck)
        {
            TpId = tpBetRecordCheck.TpId;
            CurrencyId = tpBetRecordCheck.CurrencyId;
        }

        public FinanceCheckResult(TpBetRecordCheck tpBetRecordCheck, string errorMessage)
        {
            TpId = tpBetRecordCheck.TpId;
            CurrencyId = tpBetRecordCheck.CurrencyId;
            ErrorMessage = errorMessage;
        }
    }
}
