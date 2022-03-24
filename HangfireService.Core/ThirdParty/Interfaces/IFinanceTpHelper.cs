using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Finance;
using HangfireService.Core.Models.Options;

namespace HangfireService.Core.ThirdParty.Interfaces
{
    public interface IFinanceTpHelper
    {
        int TpId { get; }

        bool CanCheckBettrans => true;

        Task<IEnumerable<FinanceCheckResult>> CheckBettrans(FinanceCheckOption checkOption, IEnumerable<TpBetRecordCheck> tpBetRecordCheckList);

        bool CanCheckTpDailySummary => true;

        Task<IEnumerable<FinanceCheckResult>> CheckTpDailySummary(FinanceCheckOption checkOption, IEnumerable<TpBetRecordCheck> tpBetRecordCheckList);
    }
}
