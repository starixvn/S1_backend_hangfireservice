using HangfireService.Core.Models.Promotion;

namespace HangfireService.Core.Promotion;

public interface IPromotionService
{
    Task<IEnumerable<GetBonusSummaryDataDto>> GetSummaryDataById(string sp, int id);
    Task<int> InsertReferralBonusSummary(IEnumerable<InsertBonusSummaryDetailDto> referralBonus);
}