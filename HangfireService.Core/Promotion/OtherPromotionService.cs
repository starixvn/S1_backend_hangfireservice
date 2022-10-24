using HangfireService.Core.DataLayers;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Promotion;

namespace HangfireService.Core.Promotion
{
    public class OtherPromotionService : IOtherPromotionService
    {
        private readonly MySqlClient _reportDb;
        private readonly MySqlClient _mainDb;

        public OtherPromotionService(IEnumerable<MySqlClient> sqlClients)
        {
            _mainDb = sqlClients.FirstOrDefault(a => a.DataBase == DataBase.Main);
            _reportDb = sqlClients.FirstOrDefault(a => a.DataBase == DataBase.Report);
        }
        
        public async Task<IEnumerable<AdjustmentInfoDto>> GetAdjustmentInfo(int id)
        {
            return await _reportDb.QueryAsync<AdjustmentInfoDto>("Service_OtherSummary_Get", new { _Id = id });
        }

        public async Task<int> InsertReferralBonusSummary(IEnumerable<InsertBonusSummaryDetailDto> referralBonus)
        {
            return await _mainDb.ExecuteAsync("Service_BonusSummaryDetail_Ins", referralBonus);
        }
    }
}