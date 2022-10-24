using HangfireService.Core.Models.Promotion;

namespace HangfireService.Core.Promotion
{
    public interface IVipUpgradePromotionService
    {
        Task<IEnumerable<VipLevelLogDto>> GetVipLevelLog(int id);
        Task<int> InsertReferralBonusSummary(IEnumerable<InsertBonusSummaryDetailDto> referralBonus);
    }
}