using HangfireService.Core.DataLayers;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Finance;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Service;
using HangfireService.Core.ThirdParty.Interfaces;

namespace HangfireService.Core.ThirdParty
{
    public class MGTpHelper : BaseTpHelper, IFinanceTpHelper
    {
        public int TpId { get; } = 34;


        public bool CanCheckTpDailySummary => true;

        private readonly ReportDBDataLayer _reportDbDataLayer;
        private readonly IEnumerable<TpCurrency> _tpCurrency;
        private readonly MySqlClient _dbClient;
        private readonly PartitionService _partitionService;
        private readonly FincanceService _fincanceService;

        public MGTpHelper(IServiceProvider provider) : base(provider)
        {
            _reportDbDataLayer = provider.GetRequiredService<ReportDBDataLayer>();

            _tpCurrency = _reportDbDataLayer.GetTpCurrency(TpId);

            _dbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);

            _partitionService = provider.GetRequiredService<PartitionService>();

            _fincanceService = provider.GetRequiredService<FincanceService>();
        }

        public async Task<IEnumerable<BettransCheckData>> GetBettransCheckData(FinanceCheckOption checkOption)
        {
            var BeginTime = checkOption.BeginTime.Ticks;
            var EndTime = checkOption.EndTime.Ticks;
            var tasks = new List<Task<IEnumerable<BettransCheckData>>>();
            
            //Slot
            tasks.Add(_dbClient.QueryAsync<BettransCheckData>("Finance_BettransRng_MG_Summary_Get", new { BeginTime, EndTime }));
            //Live Casino
            tasks.AddRange(_partitionService.QueryAsync<BettransCheckData>(checkOption.BeginTime, checkOption.EndTime, "Finance_BettransLC_MG_Summary_Get", new { BeginTime, EndTime }));
            
            Task.WhenAll(tasks);

            var CheckData = new List<BettransCheckData>();
            foreach (var item in tasks)
            {
                CheckData.AddRange(item.Result);
            }

            //使用TpCurrencyId組合Slot跟Live Casino資料
            return CheckData
                .GroupBy(i => i.TpCurrencyId)
                .Select( i => new BettransCheckData
                {
                    TpId = TpId,
                    TpCurrencyId = i.Key,
                    CurrencyId = _tpCurrency.First(c => c.TpCurrencyId == i.Key).CurrencyId,
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
           return await _fincanceService.GetTpDailySummary((int)SummaryType.TpBetRecord, TpId, checkOption.BeginTime, checkOption.EndTime);
        }
    }
}
