using HangfireService.Core.ThirdParty.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Finance;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Extensions;
using HangfireService.Core.Service;
using HangfireService.Core.Models.DBs;

namespace HangfireService.Core.ThirdParty
{
    internal class PG2TpHelper : BaseTpHelper, IFinanceTpHelper
    {
        public PG2TpHelper(IServiceProvider provider) : base(provider)
        {
            _partitionService = provider.GetRequiredService<PartitionService>();
            _fincanceService = provider.GetRequiredService<FincanceService>();
        }
        private readonly PartitionService _partitionService;
        private readonly FincanceService _fincanceService;

        public bool CheckTransId => false;
        public bool BindCommonCurrency => false;
        public TimeOption TimeOptionId => TimeOption.TransTime;
        public int TpId => 71;
        public async Task<IEnumerable<BettransCheckData>> GetBettransCheckData(FinanceCheckOption checkOption)
        {
            var BeginTime = checkOption.BeginTime.GetUnixTimeByDay();
            var EndTime = checkOption.EndTime.GetUnixTimeByDay() - 1;

            var tasks = new List<Task<IEnumerable<BettransCheckData>>>();
            tasks.AddRange(_partitionService.QueryAsync<BettransCheckData>(checkOption.BeginTime, checkOption.EndTime, "Finance_BettransRng_PG2_Summary_Get", new { _BeginTime = BeginTime, _EndTime = EndTime }));
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
