using HangfireService.Core.Extensions;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Finance;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Service;
using HangfireService.Core.ThirdParty.Interfaces;

namespace HangfireService.Core.ThirdParty
{
    internal class UuSlotsTpHelper : BaseTpHelper, IFinanceTpHelper
    {
        public bool CheckTransId => false;
        public bool BindCommonCurrency => false;
        public TimeOption TimeOptionId => TimeOption.SettledTime;

        public int TpId => 67;

        private readonly PartitionService _partitionService;

        public UuSlotsTpHelper(IServiceProvider provider) : base(provider)
        {
            _partitionService = provider.GetRequiredService<PartitionService>();
        }

        public async Task<IEnumerable<BettransCheckData>> GetBettransCheckData(FinanceCheckOption checkOption)
        {
            var BeginTime = checkOption.BeginTime.GetUnixTimeByDay();
            var EndTime = checkOption.EndTime.GetUnixTimeByDay() - 1;

            var tasks = new List<Task<IEnumerable<BettransCheckData>>>();
            tasks.AddRange(_partitionService.QueryAsync<BettransCheckData>(checkOption.BeginTime, checkOption.EndTime, "Finance_BettransRng_UuSlots_Summary_Get", new { _BeginTime = BeginTime, _EndTime = EndTime }));
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
                    ProgressiveBet = i.Sum(i => i.ProgressiveBet),
                    ProgressiveWin = i.Sum(i => i.ProgressiveWin),
                });
        }

        public async Task<IEnumerable<TpDailySummaryCheckData>> GetTpDailySummaryCheckData(FinanceCheckOption checkOption)
        {
            // 無wager & report api
            return new List<TpDailySummaryCheckData>();
        }
    }
}
