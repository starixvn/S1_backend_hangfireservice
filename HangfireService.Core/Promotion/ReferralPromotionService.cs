using Dapper;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Promotion;

namespace HangfireService.Core.Promotion
{
    public class ReferralPromotionService : IReferralPromotionService
    {
        private readonly MySqlClient _reportDb;
        private readonly MySqlClient _mainDb;

        public ReferralPromotionService(IEnumerable<MySqlClient> sqlClients)
        {
            _mainDb = sqlClients.FirstOrDefault(a => a.DataBase == DataBase.Main);
            _reportDb = sqlClients.FirstOrDefault(a => a.DataBase == DataBase.Report);
        }

        public async Task<IEnumerable<ReferralMLogDto>> GetReferralMLog(int id)
        {
            var p = new DynamicParameters(new { _Id = id });
            return await _reportDb.QueryAsync<ReferralMLogDto>("Service_ReferralSummary_Get", p);
        }

        public async Task<int> InsertReferralBonusSummary(IEnumerable<InsertBonusSummaryDetailDto> referralBonus)
        {
            return await _mainDb.ExecuteAsync("Service_BonusSummaryForReferral_Ins", referralBonus);
        }
    }
}