using HangfireService.Core.DataLayers;
using HangfireService.Core.Models;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Options;
using HangfireService.Core.ThirdParty.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace HangfireService.Core.ThirdParty
{
    public class MGTpHelper : BaseTpHelper, IFinanceTpHelper
    {
        public int TpId { get; } = 34;

        public bool CanCheckBettransData => true;

        public bool CanCheckThirdPartyData => true;

        private readonly ReportDBDataLayer _reportDbDataLayer;

        public MGTpHelper(IServiceProvider provider) : base(provider)
        {
            _reportDbDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
        }

        public IEnumerable<FinanceCheckResult> CheckBettransData(FinanceCheckOption checkOption, IEnumerable<TpBetRecordCheck> tpBetRecordCheckList)
        {
            return new List<FinanceCheckResult>();
        }

        public IEnumerable<FinanceCheckResult> CheckThirdPartyData(FinanceCheckOption checkOption, IEnumerable<TpBetRecordCheck> tpBetRecordCheckList)
        {
            return new List<FinanceCheckResult>();
        }
    }
}
