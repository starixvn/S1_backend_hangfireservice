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
    internal class Kiss918TpHelper : BaseTpHelper, IFinanceTpHelper
    {
        public bool CheckTransId => true;

        public TimeOption TimeOptionId => TimeOption.SettledTime;

        public int TpId { get; } = 21;

        private readonly ReportDBDataLayer _reportDbDataLayer;
        private readonly IEnumerable<TpCurrency> _tpCurrency;
        private readonly MySqlClient _dbClient;
        private readonly FincanceService _fincanceService;
        private readonly int utcTime = 0;

        public Kiss918TpHelper(IServiceProvider provider) : base(provider)
        {
            _reportDbDataLayer = provider.GetRequiredService<ReportDBDataLayer>();

            _tpCurrency = _reportDbDataLayer.GetTpCurrency(TpId);

            _dbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);

            _fincanceService = provider.GetRequiredService<FincanceService>();
        }

        public async Task<IEnumerable<BettransCheckData>> GetBettransCheckData(FinanceCheckOption checkOption)
        {
            var BeginTime = (long)(checkOption.BeginTime.AddHours(utcTime).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            var EndTime = (long)(checkOption.EndTime.AddHours(utcTime).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            var bettrans = await _dbClient.QueryAsync<BettransCheckData>("Finance_TpWagerKiss918_Summary_Get", new { _BeginTime = BeginTime, _EndTime = EndTime });
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
                tpDaily.Stake *= rate;
                tpDaily.ValidBet *= rate;
                tpDaily.Payout *= rate;
                tpDaily.WinlostAmount *= rate;
            }
            return tpDailyList;
        }

        public async Task<IEnumerable<BettransCheckTransIdData>> GetBettransCheckTransIdData(FinanceCheckTransIdOption checkOption)
        {
            var BeginTime = checkOption.BeginTime.GetUnixTime();
            var EndTime = checkOption.EndTime.GetUnixTime();

            var bettrans = await _dbClient.QueryAsync<BettransCheckTransIdData>("Finance_TpWagerKiss918_Check_TransId_Get", new { _BeginTime = BeginTime, _EndTime = EndTime });

            return (from item in bettrans
                    let exchangeRate = _tpCurrency.First(c => c.CurrencyId == item.CurrencyId).ExchangeRate
                    select new BettransCheckTransIdData
                    {
                        TpId = TpId,
                        TransId = item.TransId,
                        CurrencyId = item.CurrencyId,
                        Stake = item.Stake * exchangeRate,
                        ValidBet = item.ValidBet * exchangeRate,
                        Payout = 0,
                        WinlostAmount = item.WinlostAmount * exchangeRate,
                        Revenue = 0,
                        ProgressiveBet = item.ProgressiveBet * exchangeRate,
                        ProgressiveWin = item.ProgressiveWin * exchangeRate,
                    }).ToList();
        }
    }
}
