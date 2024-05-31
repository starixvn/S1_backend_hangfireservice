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
    internal class AGTpHelper : BaseTpHelper, IFinanceTpHelper
    {
        public bool CheckTransId => true;
        public bool BindCommonCurrency => true;

        public TimeOption TimeOptionId => TimeOption.SettledTime;

        public int TpId => 3;

        private readonly ReportDBDataLayer _reportDbDataLayer;
        private readonly IEnumerable<TpCurrency> _tpCurrency;
        private readonly MySqlClient _wagerClient;
        private readonly FincanceService _fincanceService;

        public AGTpHelper(IServiceProvider provider) : base(provider)
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
            var bettrans = await _wagerClient.QueryAsync<BettransCheckData>("Finance_TpWagerAG_Summary_Get", new { _BeginTime = BeginTime, _EndTime = EndTime });
            foreach (var item in bettrans)
            {
                var rate = _tpCurrency.First(c => c.TpCurrencyId == item.TpCurrencyId).ExchangeRate;
                item.CurrencyId = _tpCurrency.First(c => c.TpCurrencyId == item.TpCurrencyId).CurrencyId;
                item.TpId = TpId;
                item.Stake *= rate;
                item.ValidBet *= rate;
                item.Payout *= rate;
                item.WinlostAmount *= rate;
            }
            return bettrans;
        }

        public async Task<IEnumerable<TpDailySummaryCheckData>> GetTpDailySummaryCheckData(FinanceCheckOption checkOption)
        {
            var tpDailyList = await _fincanceService.GetTpDailySummary((int)SummaryType.TpBetRecord, TpId, checkOption.BeginTime, checkOption.EndTime);
            foreach (var tpDaily in tpDailyList)
            {
                var rate = _tpCurrency.First(c => c.CurrencyId == tpDaily.CurrencyId).ExchangeRate;
                tpDaily.Stake = Math.Round(tpDaily.Stake, 2, MidpointRounding.AwayFromZero) * rate;
                tpDaily.ValidBet = Math.Round(tpDaily.ValidBet, 2, MidpointRounding.AwayFromZero) * rate;
                tpDaily.Payout = Math.Round(tpDaily.Payout, 2, MidpointRounding.AwayFromZero) * rate;
                tpDaily.WinlostAmount = Math.Round(tpDaily.WinlostAmount, 2, MidpointRounding.AwayFromZero) * rate;
            }
            return tpDailyList;
        }

        public async Task<IEnumerable<BettransCheckTransIdData>> GetBettransCheckTransIdData(FinanceCheckTransIdOption checkOption)
        {
            var BeginTime = checkOption.BeginTime.GetUnixTime();
            var EndTime = checkOption.EndTime.GetUnixTime() - 1;
            int Mode = (int)checkOption.Mode;
            var TransId = checkOption.TransIds;
            if (Mode == (int)ConditionOption.TransId && TransId == "[]")
            {
                return Enumerable.Empty<BettransCheckTransIdData>();
            }

            var Parameter = new { _Mode = Mode, _BeginTime = BeginTime, _EndTime = EndTime, _TransIds = TransId };
            var bettrans = await _wagerClient.QueryAsync<BettransCheckTransIdData>("Finance_TpWagerAG_Check_TransId_Get", Parameter);

            return (from item in bettrans
                    let exchangeRate = _tpCurrency.First(c => c.TpCurrencyId == item.TpCurrencyId).ExchangeRate
                    let currencyId = _tpCurrency.First(c => c.TpCurrencyId == item.TpCurrencyId).CurrencyId
                    select new BettransCheckTransIdData
                    {
                        TpId = TpId,
                        TransId = item.TransId,
                        CurrencyId = currencyId,
                        Stake = item.Stake * exchangeRate,
                        ValidBet = item.ValidBet * exchangeRate,
                        Payout = item.Payout * exchangeRate,
                        WinlostAmount = item.WinlostAmount * exchangeRate,
                        Revenue = 0,
                        ProgressiveBet = 0,
                        ProgressiveWin = 0,
                    }).ToList();
        }

    }
}
