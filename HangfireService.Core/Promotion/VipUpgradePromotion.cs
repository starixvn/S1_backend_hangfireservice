using HangfireService.Core.Models.Options;
using HangfireService.Core.Models.Promotion;

namespace HangfireService.Core.Promotion
{
    public class VipUpgradePromotion : PromotionSummaryBase
    {
        private readonly IVipUpgradePromotionService _vipUpgradeService;
        private readonly IBonusSummaryTimerService _timerService;

        public VipUpgradePromotion(IBonusSummaryTimerService timerService, IVipUpgradePromotionService vipUpgradeService, WorkerType type)
        {
            WorkerType = type;
            _vipUpgradeService = vipUpgradeService;
            _timerService = timerService;
        }

        public override async Task<bool> Run()
        {
            var id = await _timerService.GetWorkerId((int)WorkerType);
            var dto = await _vipUpgradeService.GetVipLevelLog(id);
            var referral = dto.ToList();
            if (referral.Any())
            {
                var referralBonus = referral.Select(a => new InsertBonusSummaryDetailDto()
                {
                    CustId = a.CustId,
                    SiteId = a.SiteId,
                    CurrencyId = a.CurrencyId,
                    BonusId = "VU" + a.Id,
                    BonusCode = "VipUpgrade",
                    BonusType = 14,
                    UserName = a.UserName,
                    TransactionType = 0,
                    Amount = a.Amount,
                    Rollover = 0,
                    Turnover = 0,
                    ClaimDate = a.CreateTime
                });
            
                var result = await _vipUpgradeService.InsertReferralBonusSummary(referralBonus);
                if (result > 1)
                {
                    var lastId = referral.Max(a => a.Id);
                    await _timerService.SetWorkerId((int)WorkerType, "VipUpgrade", lastId);
                }
            }
            return true;
        }
    }
}