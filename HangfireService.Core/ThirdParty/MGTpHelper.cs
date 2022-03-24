using HangfireService.Core.DataLayers;
using HangfireService.Core.Models;
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
        private readonly Dictionary<int, TpCurrency> _tpCurrency;
        private readonly MySqlClient _dbClient;
        private readonly PartitionService _partitionService;
        private readonly FincanceService _fincanceService;

        public MGTpHelper(IServiceProvider provider) : base(provider)
        {
            _reportDbDataLayer = provider.GetRequiredService<ReportDBDataLayer>();

            _tpCurrency = _reportDbDataLayer.GetTpCurrency(TpId).ToDictionary(i => i.CurrencyId, i => i);

            _dbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);

            _partitionService = provider.GetRequiredService<PartitionService>();

            _fincanceService = provider.GetRequiredService<FincanceService>();
        }

        public async Task<IEnumerable<FinanceCheckResult>> CheckBettrans(FinanceCheckOption checkOption, IEnumerable<TpBetRecordCheck> tpBetRecordCheckList)
        {
            var result = new List<FinanceCheckResult>();
            
            var BeginTime = checkOption.BeginTime.Ticks;
            var EndTime = checkOption.EndTime.Ticks;
            
            var CheckData = new List<CheckData>();
            //Slot
            CheckData.AddRange(_dbClient.Query<CheckData>("Finance_BettransRng_MG_Summary_Get", new { BeginTime, EndTime }));

            //Live Casino
            //CheckData.AddRange(_partitionService.Query<CheckData>(checkOption.BeginTime, checkOption.EndTime, "Finance_BettransLC_MG_Summary_Get", new { BeginTime, EndTime }));

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

                var checkResult = tpBetRecord.Check(bettransData);
                if (checkResult != null)    
                {
                    result.Add(checkResult);
                }
            }
            return result;
        }

        public async Task<IEnumerable<FinanceCheckResult>> CheckTpDailySummary(FinanceCheckOption checkOption, IEnumerable<TpBetRecordCheck> tpBetRecordCheckList)
        {
            var result = new List<FinanceCheckResult>();

            var CheckDataList = _fincanceService.GetTpDailySummary((int)SummaryType.TpBetRecord, TpId, checkOption.BeginTime, checkOption.EndTime);
            if (tpBetRecordCheckList.Count() != CheckDataList.Count())
            {
                result.Add(new FinanceCheckResult(TpId, $"TpBetRecord & TpDailySummary count no match."));
                return result;
            }

            foreach (var tpBetRecord in tpBetRecordCheckList)
            {         
                var checkData = CheckDataList.FirstOrDefault(i => i.CurrencyId == tpBetRecord.CurrencyId);
                if (checkData == null)
                {
                    result.Add(new FinanceCheckResult(tpBetRecord, $"{tpBetRecord.CurrencyId} TpDailySummary no found."));
                    continue;
                }

                var checkResult = tpBetRecord.Check(checkData);
                if (checkResult != null)
                {
                    result.Add(checkResult);
                }
            }
            
            return result;
        }


       
    }
}
