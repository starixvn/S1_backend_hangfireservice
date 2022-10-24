using HangfireService.Core.Models.Promotion;

namespace HangfireService.Core.Promotion
{
    public interface IReferralFreeCreditPromotionService
    {
        Task<IEnumerable<ReferralLogDto>> GetReferralLog(DateTime startTime, DateTime endTime);
        Task<int> InsertReferralBonusSummary(IEnumerable<InsertBonusSummaryDetailDto> referralBonus);
    }
}