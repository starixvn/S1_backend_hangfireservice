using HangfireService.Core.DataLayers;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Promotion;

namespace HangfireService.Core.Promotion
{
    public class PromotionService : IPromotionService
    {
        private readonly MySqlClient _reportDb;
        private readonly MySqlClient _mainDb;

        public PromotionService(IEnumerable<MySqlClient> sqlClients)
        {
            _mainDb = sqlClients.FirstOrDefault(a => a.DataBase == DataBase.Main);
            _reportDb = sqlClients.FirstOrDefault(a => a.DataBase == DataBase.Report);
        }
        
        public async Task<IEnumerable<GetBonusSummaryDataByIdDto>> GetSummaryDataById(string sp, int id)
        {
            return await _reportDb.QueryAsync<GetBonusSummaryDataByIdDto>(sp, new { _Id = id });
        }

        public async Task<IEnumerable<GetBonusSummaryDataByTimeDto>> GetSummaryDataByTime(string sp, DateTime startTime, DateTime endTime)
        {
            return await _reportDb.QueryAsync<GetBonusSummaryDataByTimeDto>(sp, new { _StartTime = startTime, _EndTime = endTime });
        }
        
        public async Task<IEnumerable<InsertBonusSummaryInfoDto>> GetBonusSummaryDetailByTime(string sp, DateTime startTime, DateTime endTime)
        {
            return await _reportDb.QueryAsync<InsertBonusSummaryInfoDto>(sp, new { _StartTime = startTime, _EndTime = endTime });
        }

        public async Task<int> InsertReferralBonusSummary(IEnumerable<InsertBonusSummaryDetailDto> referralBonus)
        {
            return await _mainDb.ExecuteAsync("Service_BonusSummaryDetail_Ins", referralBonus);
        }
        
        public async Task<int> InsertBonusSummaryInfo(IEnumerable<InsertBonusSummaryInfoDto> bonusSummaryInfo)
        {
            return await _mainDb.ExecuteAsync("Service_BonusSummaryInfo_Ins", bonusSummaryInfo);
        }
    }
}