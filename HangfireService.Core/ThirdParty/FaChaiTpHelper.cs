using HangfireService.Core.DataLayers;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Finance;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Service;
using HangfireService.Core.ThirdParty.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HangfireService.Core.ThirdParty
{
    public class FaChaiTpHelper : BaseTpHelper, IFinanceTpHelper
    {
        public bool CheckTransId => false;
        public bool BindCommonCurrency => true;
        public TimeOption TimeOptionId => TimeOption.TransTime;

        public int TpId => 55;

        private readonly ReportDBDataLayer _reportDbDataLayer;
        private readonly IEnumerable<TpCurrency> _tpCurrency;
        private readonly MySqlClient _wagerClient;
        private readonly FincanceService _fincanceService;
        public FaChaiTpHelper(IServiceProvider provider) : base(provider)
        {
            _reportDbDataLayer = provider.GetRequiredService<ReportDBDataLayer>();

            _tpCurrency = _reportDbDataLayer.GetTpCurrency(TpId);

            _wagerClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);

            _fincanceService = provider.GetRequiredService<FincanceService>();
        }

        public Task<IEnumerable<BettransCheckData>> GetBettransCheckData(FinanceCheckOption checkOption)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<TpDailySummaryCheckData>> GetTpDailySummaryCheckData(FinanceCheckOption checkOption)
        {
            throw new NotImplementedException();
        }
    }
}
