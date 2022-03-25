using HangfireService.Core.Models.DBs;
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
}
