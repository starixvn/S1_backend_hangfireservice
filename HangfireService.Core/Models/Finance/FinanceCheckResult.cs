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
}
