using HangfireService.Core.DataLayers;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Promotion;

namespace HangfireService.Core.Promotion
{
    public class ReferralFreeCreditPromotionService : IReferralFreeCreditPromotionService
    {
        private readonly MySqlClient _reportDb;
        private readonly MySqlClient _mainDb;

        public ReferralFreeCreditPromotionService(IEnumerable<MySqlClient> sqlClients)
        {
            _mainDb = sqlClients.FirstOrDefault(a => a.DataBase == DataBase.Main);
            _reportDb = sqlClients.FirstOrDefault(a => a.DataBase == DataBase.Report);
        }

        public async Task<IEnumerable<ReferralLogDto>> GetReferralLog(DateTime startTime, DateTime endTime)
        {
            return await _reportDb.QueryAsync<ReferralLogDto>("Service_ReferralFreeCreditSummary_Get", 
                new { _StartTime = startTime, _EndTime = endTime });
        }

        public async Task<int> InsertReferralBonusSummary(IEnumerable<InsertBonusSummaryDetailDto> referralBonus)
        {
            return await _mainDb.ExecuteAsync("Service_BonusSummaryDetail_Ins", referralBonus);
        }
    }
}