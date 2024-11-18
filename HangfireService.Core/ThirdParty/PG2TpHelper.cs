using HangfireService.Core.ThirdParty.Interfaces;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Finance;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Service;
using HangfireService.Core.DataLayers;

namespace HangfireService.Core.ThirdParty
{
    internal class PG2TpHelper : BaseTpHelper, IFinanceTpHelper
    {
        public PG2TpHelper(IServiceProvider provider) : base(provider)
        {
            _fincanceService = provider.GetRequiredService<FincanceService>();
            _dbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        private readonly FincanceService _fincanceService;
        private readonly MySqlClient _dbClient;

        public bool CheckTransId => false;
        public bool BindCommonCurrency => false;
        public TimeOption TimeOptionId => TimeOption.TransTime;
        public int TpId => 71;
        public async Task<IEnumerable<BettransCheckData>> GetBettransCheckData(FinanceCheckOption checkOption)
        {
            var BeginTime = checkOption.BeginTime;
            var EndTime = checkOption.EndTime;

            var tasks = new List<Task<IEnumerable<BettransCheckData>>>();
            tasks.Add(_dbClient.QueryAsync<BettransCheckData>( "Finance_BettransRng_PG2_Summary_Get", new { _BeginTime = BeginTime, _EndTime = EndTime },120));
            await Task.WhenAll(tasks);

            var CheckData = new List<BettransCheckData>();
            foreach (var item in tasks)
            {
                var data = item.Result;
                CheckData.AddRange(data);
            }

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
            
            return tpDailyList;
        }
    }
}
