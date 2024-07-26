using HangfireService.Core.DataLayers;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Finance;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Service;
using HangfireService.Core.ThirdParty.Interfaces;

namespace HangfireService.Core.ThirdParty
{
    internal class SkywindHelper : BaseTpHelper, IFinanceTpHelper
    {
        public bool CheckTransId => false;
        public bool BindCommonCurrency => true;

        public TimeOption TimeOptionId => TimeOption.SettledTime;

        public int TpId => 43;

        private readonly ReportDBDataLayer _reportDbDataLayer;
        private readonly IEnumerable<TpCurrency> _tpCurrency;
        private readonly FincanceService _fincanceService;
        private readonly MySqlClient _wagerClient;
        public SkywindHelper(IServiceProvider provider) : base(provider)
        {
            _reportDbDataLayer = provider.GetRequiredService<ReportDBDataLayer>();

            _tpCurrency = _reportDbDataLayer.GetTpCurrency(TpId);

            _fincanceService = provider.GetRequiredService<FincanceService>();

            _wagerClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public async Task<IEnumerable<BettransCheckData>> GetBettransCheckData(FinanceCheckOption checkOption)
        {
            var BeginTime = (long)(checkOption.BeginTime.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            var EndTime = (long)(checkOption.EndTime.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            var bettrans = await _wagerClient.QueryAsync<BettransCheckData>("Finance_TpWagerSkywind_Summary_Get", new { _BeginTime = BeginTime, _EndTime = EndTime });
            foreach (var item in bettrans)
            {
                var rate = _tpCurrency.First(c => c.TpCurrencyId == item.TpCurrencyId).ExchangeRate;
                item.CurrencyId = _tpCurrency.First(c => c.TpCurrencyId == item.TpCurrencyId).CurrencyId;
                item.TpId = TpId;
                item.Stake *= rate;
                item.ValidBet *= rate;
                item.Payout *= rate;
                item.WinlostAmount *= rate;
                item.ProgressiveBet *= rate;
                item.ProgressiveWin *= rate;
            }
            return bettrans;
        }
        public async Task<IEnumerable<TpDailySummaryCheckData>> GetTpDailySummaryCheckData(FinanceCheckOption checkOption)
        {
            var tpDailyList = await _fincanceService.GetTpDailySummary((int)SummaryType.TpBetRecord, TpId, checkOption.BeginTime, checkOption.EndTime);
            foreach (var tpDaily in tpDailyList)
            {
                var rate = _tpCurrency.First(c => c.CurrencyId == tpDaily.CurrencyId).ExchangeRate;
                tpDaily.Stake *= rate;
                tpDaily.ValidBet *= rate;
                tpDaily.Payout *= rate;
                tpDaily.WinlostAmount *= rate;
                tpDaily.ProgressiveBet *= rate;
                tpDaily.ProgressiveWin *= rate;
            }
            return tpDailyList;
        }
    }
}
