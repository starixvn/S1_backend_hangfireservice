using HangfireService.Core.Models.Promotion;

namespace HangfireService.Core.Promotion
{
    public interface IReferralPromotionService
    {
        Task<IEnumerable<ReferralMLogDto>> GetReferralMLog(int id);
        Task<int> InsertReferralBonusSummary(IEnumerable<InsertBonusSummaryDetailDto> referralBonus);
    }
}