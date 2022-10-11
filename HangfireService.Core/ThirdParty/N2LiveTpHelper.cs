using HangfireService.Core.DataLayers;
using HangfireService.Core.Extensions;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Finance;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Service;
using HangfireService.Core.ThirdParty.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HangfireService.Core.ThirdParty
{
    internal class N2LiveTpHelper : BaseTpHelper, IFinanceTpHelper
    {
        public int TpId => 28;

        private readonly ReportDBDataLayer _reportDbDataLayer;
        private readonly IEnumerable<TpCurrency> _tpCurrency;
        private readonly MySqlClient _wagerClient;
        private readonly MySqlClient _dbClient;
        private readonly FincanceService _fincanceService;

        public N2LiveTpHelper(IServiceProvider provider) : base(provider)
        {
            _reportDbDataLayer = provider.GetRequiredService<ReportDBDataLayer>();

            _tpCurrency = _reportDbDataLayer.GetTpCurrency(TpId);

            _wagerClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);

            _dbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);

            _fincanceService = provider.GetRequiredService<FincanceService>();
        }

        public async Task<IEnumerable<BettransCheckData>> GetBettransCheckData(FinanceCheckOption checkOption)
        {
            var BeginTime = checkOption.BeginTime.GetUnixTimeByDay();
            var EndTime = checkOption.EndTime.GetUnixTimeByDay() - 1;

            var tasks = new List<Task<IEnumerable<BettransCheckData>>>
            {
                _wagerClient.QueryAsync<BettransCheckData>("Finance_TpSmartWagerLC_N2Live_Summary_Get", new { _BeginTime = BeginTime, _EndTime = EndTime }),
                _wagerClient.QueryAsync<BettransCheckData>("Finance_TpWagerLC_N2Live_Summary_Get", new { _BeginTime = BeginTime, _EndTime = EndTime })
            };

            await Task.WhenAll(tasks);

            var CheckData = new List<BettransCheckData>();
            foreach (var item in tasks)
            {
                CheckData.AddRange(item.Result);
            }

            //使用CurrencyId組合資料
            var bettransData = CheckData
                .GroupBy(i => i.CurrencyId)
                .Select(i => new BettransCheckData
                {
                    TpId = TpId,
                    TpCurrencyId = _tpCurrency.First(c => c.CurrencyId == i.Key).TpCurrencyId,
                    CurrencyId = i.Key,
                    Stake = i.Sum(i => i.Stake) * _tpCurrency.First(c => c.CurrencyId == i.Key).ExchangeRate,
                    ValidBet = i.Sum(i => i.ValidBet) * _tpCurrency.First(c => c.CurrencyId == i.Key).ExchangeRate,
                    Payout = i.Sum(i => i.Payout) * _tpCurrency.First(c => c.CurrencyId == i.Key).ExchangeRate,
                    WinlostAmount = i.Sum(i => i.WinlostAmount) * _tpCurrency.First(c => c.CurrencyId == i.Key).ExchangeRate,
                    Revenue = 0,
                    ProgressiveBet = 0,
                    ProgressiveWin = 0,
                });
            
            return bettransData;
        }

        public async Task<IEnumerable<TpDailySummaryCheckData>> GetTpDailySummaryCheckData(FinanceCheckOption checkOption)
        {
            // N2Live無報表API 資料為Wager總計
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
    }
}
