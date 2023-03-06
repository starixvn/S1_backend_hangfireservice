using HangfireService.Core.Models.Promotion;

namespace HangfireService.Core.Promotion;

public interface IPromotionService
{
    Task<IEnumerable<GetBonusSummaryDataByIdDto>> GetSummaryDataById(string sp, int id);
    IEnumerable<GetBonusSummaryDataByIdDto> GetSummaryDataById(string sp, int id, out int maxCheckId);
    Task<IEnumerable<GetBonusSummaryDataByTimeDto>> GetSummaryDataByTime(string sp, DateTime startTime, DateTime endTime);
    Task<IEnumerable<InsertBonusSummaryInfoDto>> GetBonusSummaryDetailByTime(string sp, DateTime startTime, DateTime endTime);
    Task<int> InsertBonusSummaryDetail(IEnumerable<InsertBonusSummaryDetailDto> referralBonus);
    Task<int> InsertBonusSummaryInfo(IEnumerable<InsertBonusSummaryInfoDto> bonusSummaryInfo);
}