using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Finance;
using HangfireService.Core.Models.Options;

namespace HangfireService.Core.ThirdParty.Interfaces
{
    public interface IFinanceTpHelper
    {
        int TpId { get; }

        bool CanCheckBettransData => true;

        Task<IEnumerable<FinanceCheckResult>> CheckBettransData(FinanceCheckOption checkOption, IEnumerable<TpBetRecordCheck> tpBetRecordCheckList);

        bool CanCheckThirdPartyData => true;
        Task<IEnumerable<FinanceCheckResult>> CheckThirdPartyData(FinanceCheckOption checkOption, IEnumerable<TpBetRecordCheck> tpBetRecordCheckList);
    }
}
