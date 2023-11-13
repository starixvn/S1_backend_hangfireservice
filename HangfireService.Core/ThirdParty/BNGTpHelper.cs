using HangfireService.Core.DataLayers;
using HangfireService.Core.Extensions;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Finance;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Service;
using HangfireService.Core.ThirdParty.Interfaces;

namespace HangfireService.Core.ThirdParty
{
    public class BNGTpHelper : BaseTpHelper, IFinanceTpHelper
    {
        public bool CheckTransId => false;
        public bool BindCommonCurrency => true;
        public TimeOption TimeOptionId => TimeOption.TransTime;

        public int TpId => 57;

        private readonly ReportDBDataLayer _reportDbDataLayer;
        private readonly IEnumerable<TpCurrency> _tpCurrency;
        private readonly MySqlClient _wagerClient;
        private readonly FincanceService _fincanceService;

        public BNGTpHelper(IServiceProvider provider) : base(provider)
        {
            _reportDbDataLayer = provider.GetRequiredService<ReportDBDataLayer>();

            _tpCurrency = _reportDbDataLayer.GetTpCurrency(TpId);

            _wagerClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);

            _fincanceService = provider.GetRequiredService<FincanceService>();
        }

        public async Task<IEnumerable<BettransCheckData>> GetBettransCheckData(FinanceCheckOption checkOption)
        {
            var BeginTime = checkOption.BeginTime.GetUnixTimeByDay();
            var EndTime = checkOption.EndTime.GetUnixTimeByDay() - 1;
            var bettrans = await _wagerClient.QueryAsync<BettransCheckData>("Finance_TpWager_BNG_Summary_Get", new { _BeginTime = BeginTime, _EndTime = EndTime });

            foreach (var item in bettrans)
            {
                var rate = _tpCurrency.First(c => c.CurrencyId == item.CurrencyId).ExchangeRate;
                item.TpId = TpId;
                item.Stake *= rate;
                item.ValidBet *= rate;
                item.WinlostAmount *= rate;
                item.Payout = (item.Stake + item.WinlostAmount);
            }
            return bettrans;
        }

        public async Task<IEnumerable<TpDailySummaryCheckData>> GetTpDailySummaryCheckData(FinanceCheckOption checkOption)
        {
            // BNG無報表API 資料為Wager總計
            var tpDailyList = await _fincanceService.GetTpDailySummary((int)SummaryType.TpBetRecord, TpId, checkOption.BeginTime, checkOption.EndTime);
            foreach (var tpDaily in tpDailyList)
            {
                var rate = _tpCurrency.First(c => c.CurrencyId == tpDaily.CurrencyId).ExchangeRate;
                tpDaily.Stake *= rate;
                tpDaily.ValidBet *= rate;
                tpDaily.WinlostAmount *= rate;
                tpDaily.Payout *= rate;
            }
            return tpDailyList;
        }
    }
}
