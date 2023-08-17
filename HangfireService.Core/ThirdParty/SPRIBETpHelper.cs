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
    internal class SPRIBETpHelper : BaseTpHelper, IFinanceTpHelper
    {
        public bool CheckTransId => false;
        public bool BindCommonCurrency => true;

        public TimeOption TimeOptionId => TimeOption.SettledTime;

        public int TpId => 53;

        private readonly ReportDBDataLayer _reportDbDataLayer;
        private readonly IEnumerable<TpCurrency> _tpCurrency;
        private readonly FincanceService _financeService;
        private readonly PartitionService _partitionService;
        public SPRIBETpHelper(IServiceProvider provider) : base(provider)
        {
            _reportDbDataLayer = provider.GetRequiredService<ReportDBDataLayer>();

            _tpCurrency = _reportDbDataLayer.GetTpCurrency(TpId);

            _partitionService = provider.GetRequiredService<PartitionService>();

            _financeService = provider.GetRequiredService<FincanceService>();
        }

        public async Task<IEnumerable<BettransCheckData>> GetBettransCheckData(FinanceCheckOption checkOption)
        {
            var tasks = new List<Task<IEnumerable<BettransCheckData>>>();
            tasks.AddRange(_partitionService.QueryAsync<BettransCheckData>(checkOption.BeginTime, checkOption.EndTime, "Finance_BettransRng_Spribe_Summary_Get", new { _BeginTime = checkOption.BeginTime, _EndTime = checkOption.EndTime }, 120));
            await Task.WhenAll(tasks);
        
            var checkData = new List<BettransCheckData>();
            foreach (var item in tasks)
            {
                var data = item.Result;
                data = ProcessBettransSubCurrencyToMainCurrency(data);
                checkData.AddRange(data);
            }

            return checkData
                .GroupBy(i => i.CurrencyId)
                .Select(i => new BettransCheckData
                {
                    TpId = TpId,
                    CurrencyId = _tpCurrency.First(c => c.CurrencyId == i.Key).CurrencyId,
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
            var tpDailyList = await _financeService.GetTpDailySummary((int)SummaryType.TpBetRecord, TpId, checkOption.BeginTime, checkOption.EndTime);
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
