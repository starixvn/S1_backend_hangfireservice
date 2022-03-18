using HangfireService.Core.Models;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HangfireService.Core.ThirdParty.Interfaces
{
    public interface IFinanceTpHelper
    {
        int TpId { get; }

        bool CanCheckBettransData { get; }

        IEnumerable<FinanceCheckResult> CheckBettransData(FinanceCheckOption checkOption, IEnumerable<TpBetRecordCheck> tpBetRecordCheckList);

        bool CanCheckThirdPartyData { get; }
        IEnumerable<FinanceCheckResult> CheckThirdPartyData(FinanceCheckOption checkOption, IEnumerable<TpBetRecordCheck> tpBetRecordCheckList);
    }
}
