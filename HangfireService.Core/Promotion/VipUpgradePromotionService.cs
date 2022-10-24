using Dapper;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Promotion;

namespace HangfireService.Core.Promotion
{
    public class VipUpgradePromotionService : IVipUpgradePromotionService
    {
        private readonly MySqlClient _reportDb;
        private readonly MySqlClient _mainDb;

        public VipUpgradePromotionService(IEnumerable<MySqlClient> sqlClients)
        {
            _mainDb = sqlClients.FirstOrDefault(a => a.DataBase == DataBase.Main);
            _reportDb = sqlClients.FirstOrDefault(a => a.DataBase == DataBase.Report);
        }

        public async Task<IEnumerable<VipLevelLogDto>> GetVipLevelLog(int id)
        {
            return await _reportDb.QueryAsync<VipLevelLogDto>("Service_VipUpgradeSummary_Get", new { _Id = id });
        }

        public async Task<int> InsertReferralBonusSummary(IEnumerable<InsertBonusSummaryDetailDto> referralBonus)
        {
            return await _mainDb.ExecuteAsync("Service_BonusSummaryForReferral_Ins", referralBonus);
        }
    }
}