using HangfireService.Core.Models.Promotion;

namespace HangfireService.Core.Promotion
{
    public interface IOtherPromotionService
    {
        Task<IEnumerable<AdjustmentInfoDto>> GetAdjustmentInfo(int id);
        Task<int> InsertReferralBonusSummary(IEnumerable<InsertBonusSummaryDetailDto> referralBonus);
    }
}
