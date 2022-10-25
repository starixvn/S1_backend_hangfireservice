using HangfireService.Core.Models.Promotion;

namespace HangfireService.Core.Promotion;

public interface IPromotionService
{
    Task<IEnumerable<GetBonusSummaryDataByIdDto>> GetSummaryDataById(string sp, int id);

    Task<IEnumerable<GetBonusSummaryDataByTimeDto>> GetSummaryDataByTime(string sp, DateTime startTime, DateTime endTime);
    Task<int> InsertReferralBonusSummary(IEnumerable<InsertBonusSummaryDetailDto> referralBonus);
}