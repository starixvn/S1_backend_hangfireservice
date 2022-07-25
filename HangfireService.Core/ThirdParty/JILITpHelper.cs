using HangfireService.Core.DataLayers;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Finance;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Service;
using HangfireService.Core.ThirdParty.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HangfireService.Core.ThirdParty
{
    public class JILITpHelper : BaseTpHelper, IFinanceTpHelper
    {
        public int TpId => 32;

        private readonly ReportDBDataLayer _reportDbDataLayer;
        private readonly IEnumerable<TpCurrency> _tpCurrency;
        private readonly MySqlClient _dbClient;
        private readonly FincanceService _fincanceService;
        public JILITpHelper(IServiceProvider provider) : base(provider)
        {
            _reportDbDataLayer = provider.GetRequiredService<ReportDBDataLayer>();

            _tpCurrency = _reportDbDataLayer.GetTpCurrency(TpId);

            _dbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);

            _fincanceService = provider.GetRequiredService<FincanceService>();
        }

        public async Task<IEnumerable<BettransCheckData>> GetBettransCheckData(FinanceCheckOption checkOption)
        {
            var BeginTime = (long)(checkOption.BeginTime.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            var EndTime = (long)(checkOption.EndTime.Subtract(new DateTime(1970, 1, 1))).TotalSeconds -1;
            var bettrans = await _dbClient.QueryAsync<BettransCheckData>("Finance_BettransRng_JILI_Summary_Get", new { _BeginTime = BeginTime, _EndTime = EndTime });
            foreach (var item in bettrans)
            {
                item.TpId = TpId;
            }
            return bettrans;
        }

        public async Task<IEnumerable<TpDailySummaryCheckData>> GetTpDailySummaryCheckData(FinanceCheckOption checkOption)
        {
            var tpDailyList = await _fincanceService.GetTpDailySummary((int)SummaryType.TpBetRecord, TpId, checkOption.BeginTime, checkOption.EndTime);
            foreach (var tpDaily in tpDailyList)
            {
                var rate = _tpCurrency.First(c => c.CurrencyId == tpDaily.CurrencyId).ExchangeRate;
                tpDaily.Stake = (tpDaily.Stake * -1) * rate;
                tpDaily.ValidBet = (tpDaily.ValidBet * -1) * rate;
                tpDaily.Payout *= rate;
                tpDaily.WinlostAmount = tpDaily.WinlostAmount * rate;
            }
            return tpDailyList;
        }
    }
}
