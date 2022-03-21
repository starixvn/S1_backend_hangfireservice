using HangfireService.Core.DataLayers;
using HangfireService.Core.Models;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Finance;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Service;
using HangfireService.Core.ThirdParty.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace HangfireService.Core.ThirdParty
{
    public class MGTpHelper : BaseTpHelper, IFinanceTpHelper
    {
        public int TpId { get; } = 34;


        public bool CanCheckThirdPartyData => true;

        private readonly ReportDBDataLayer _reportDbDataLayer;
        private readonly Dictionary<int, TpCurrency> _tpCurrency;
        private readonly MySqlClient _dbClient;
        private readonly PartitionService partitionService;

        public MGTpHelper(IServiceProvider provider) : base(provider)
        {
            _reportDbDataLayer = provider.GetRequiredService<ReportDBDataLayer>();

            _tpCurrency = _reportDbDataLayer.GetTpCurrency(TpId).ToDictionary(i => i.CurrencyId, i => i);

            _dbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);

            partitionService = provider.GetRequiredService<PartitionService>();
        }

        public async Task<IEnumerable<FinanceCheckResult>> CheckBettransData(FinanceCheckOption checkOption, IEnumerable<TpBetRecordCheck> tpBetRecordCheckList)
        {
            var result = new List<FinanceCheckResult>();
            
            var BeginTime = checkOption.BeginTime.Ticks;
            var EndTime = checkOption.EndTime.Ticks;
            
            var CheckData = new List<CheckData>();
            //Slot
            CheckData.AddRange(_dbClient.Query<CheckData>("Service_BettransRng_MG_Summary_Get", new { BeginTime, EndTime }));
            
            //Live Casino
            //CheckData.AddRange(partitionService.Query<CheckData>(checkOption.BeginTime, checkOption.EndTime, "Service_BettransLC_MG_Summary_Get", new { BeginTime, EndTime }));

            //使用TpCurrencyId組合Slot跟Live Casino資料
            var bettransDataList = CheckData.GroupBy(i => i.TpCurrencyId)
                .ToDictionary(i => i.Key, i => new CheckData 
                { 
                    TpCurrencyId = i.First().TpCurrencyId ,
                    Stake = i.Sum(i => i.Stake),
                    ValidBet = i.Sum(i => i.ValidBet),
                    Payout = i.Sum(i => i.Payout),
                    WinlostAmount = i.Sum(i => i.WinlostAmount)                
                });

            if (tpBetRecordCheckList.Count() != bettransDataList.Count)
            {
                result.Add(new FinanceCheckResult(TpId, $"TpBetRecord & BettransData group by Currency count no match."));
                return result;
            }

            foreach (var tpBetRecord in tpBetRecordCheckList)
            {
                var tpCurrencyId = _tpCurrency[tpBetRecord.CurrencyId].TpCurrencyId;
                if (!bettransDataList.TryGetValue(tpCurrencyId, out var bettransData))
                {
                    result.Add(new FinanceCheckResult(tpBetRecord, $"{tpCurrencyId} bettransData no found."));
                    continue;
                }

                var checkResult = tpBetRecord.Check(FinanceCheckType.BettransData, bettransData);
                if (checkResult != null)    
                {
                    result.Add(checkResult);
                }
            }
            return result;
        }

        public async Task<IEnumerable<FinanceCheckResult>> CheckThirdPartyData(FinanceCheckOption checkOption, IEnumerable<TpBetRecordCheck> tpBetRecordCheckList)
        {
            return new List<FinanceCheckResult>();
        }


       
    }
}
