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
    internal class CQ9TpHelper : BaseTpHelper, IFinanceTpHelper
    {
        public int TpId => 16;

        private readonly ReportDBDataLayer _reportDbDataLayer;
        private readonly IEnumerable<TpCurrency> _tpCurrency;
        private readonly MySqlClient _dbClient;
        private readonly FincanceService _fincanceService;

        public CQ9TpHelper(IServiceProvider provider) : base(provider)
        {
            _reportDbDataLayer = provider.GetRequiredService<ReportDBDataLayer>();

            _tpCurrency = _reportDbDataLayer.GetTpCurrency(TpId);

            _dbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);

            _fincanceService = provider.GetRequiredService<FincanceService>();
        }

        public async Task<IEnumerable<BettransCheckData>> GetBettransCheckData(FinanceCheckOption checkOption)
        {
            var BeginTime = checkOption.BeginTime.GetUnixTimeByDay();
            var EndTime = checkOption.EndTime.GetUnixTimeByDay();

            var tasks = new List<Task<IEnumerable<BettransCheckData>>>();
            tasks.Add(_dbClient.QueryAsync<BettransCheckData>("Finance_BettransFish_CQ9_Summary_Get", new { _BeginTime = BeginTime, _EndTime = EndTime }));
            tasks.Add(_dbClient.QueryAsync<BettransCheckData>("Finance_BettransRNG_CQ9_Summary_Get", new { _BeginTime = BeginTime, _EndTime = EndTime }));
            await Task.WhenAll(tasks);

            var CheckData = new List<BettransCheckData>();
            foreach (var item in tasks)
            {
                CheckData.AddRange(item.Result);
            }

            //使用TpCurrencyId組合Slot跟Fish資料
            return CheckData
                .GroupBy(i => i.CurrencyId)
                .Select(i => new BettransCheckData
                {
                    TpId = TpId,
                    CurrencyId = i.Key,
                    Stake = i.Sum(i => i.Stake),
                    ValidBet = i.Sum(i => i.ValidBet),
                    Payout = i.Sum(i => i.Payout),
                    WinlostAmount = i.Sum(i => i.WinlostAmount),
                    Revenue = 0,
                    ProgressiveBet = 0,
                    ProgressiveWin = 0,
                });
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
    }
}
