using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Finance;
using HangfireService.Core.Models.Options;

namespace HangfireService.Core.ThirdParty.Interfaces
{
    public interface IFinanceTpHelper
    {
        int TpId { get; }

        Task<IEnumerable<BettransCheckData>> GetBettransCheckData(FinanceCheckOption checkOption);

        Task<IEnumerable<TpDailySummaryCheckData>> GetTpDailySummaryCheckData(FinanceCheckOption checkOption);
    }
}
