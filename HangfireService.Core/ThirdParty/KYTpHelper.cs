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
    internal class KYTpHelper : BaseTpHelper, IFinanceTpHelper
    {
        public bool CheckTransId => false;
        public bool BindCommonCurrency => false;
        public TimeOption TimeOptionId => TimeOption.SettledTime;
        public int TpId => 73;

        private readonly ReportDBDataLayer _reportDbDataLayer;
        private readonly IEnumerable<TpCurrency> _tpCurrency;
        private readonly MySqlClient _wagerClient;
        private readonly FincanceService _fincanceService;

        public KYTpHelper(IServiceProvider provider) : base(provider)
        {
            _reportDbDataLayer = provider.GetRequiredService<ReportDBDataLayer>();

            _tpCurrency = _reportDbDataLayer.GetTpCurrency(TpId);

            _wagerClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);

            _fincanceService = provider.GetRequiredService<FincanceService>();
        }

        // TODO: FinanceTPHelperTests
        public async Task<IEnumerable<BettransCheckData>> GetBettransCheckData(FinanceCheckOption checkOption)
        {
            // TODO: KY 為轉帳遊戲無Bettrans 改取Wager資料 (跟 V8 一致?)
            var BeginTime = checkOption.BeginTime.GetUnixTimeByDay();
            var EndTime = checkOption.EndTime.GetUnixTimeByDay() - 1;
            var bettrans = await _wagerClient.QueryAsync<BettransCheckData>("Finance_TpWagerPK_KY_Summary_Get", new { _BeginTime = BeginTime, _EndTime = EndTime });
            bettrans = ProcessBettransSubCurrencyToMainCurrency(bettrans);
            foreach (var item in bettrans)
            {
                var rate = _tpCurrency.First(c => c.CurrencyId == item.CurrencyId).ExchangeRate;
                item.TpId = TpId;
                item.Stake *= rate;
                item.ValidBet *= rate;
                item.Payout *= rate;
                item.WinlostAmount *= rate;
                item.Revenue *= rate;
            }
            return bettrans;
        }

        // TODO: FinanceTPHelperTests
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
                tpDaily.Revenue *= rate;
            }
            return tpDailyList;
        }
    }
}
