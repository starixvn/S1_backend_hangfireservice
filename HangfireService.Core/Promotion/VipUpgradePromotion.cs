using HangfireService.Core.Models.Options;
using HangfireService.Core.Models.Promotion;

namespace HangfireService.Core.Promotion
{
    public class VipUpgradePromotion : PromotionSummaryBase
    {
        private readonly IPromotionService _promotionService;
        private readonly IBonusSummaryTimerService _timerService;

        public VipUpgradePromotion(IBonusSummaryTimerService timerService, IPromotionService promotionService, WorkerType type)
        {
            WorkerType = type;
            _promotionService = promotionService;
            _timerService = timerService;
        }

        public override async Task<bool> Run()
        {
            var id = await _timerService.GetWorkerId((int)WorkerType);
            var dto = await _promotionService.GetSummaryDataById("Service_VipUpgradeSummary_Get", id);
            var referral = dto.ToList();
            if (referral.Any())
            {
                var referralBonus = referral.Select(a => new InsertBonusSummaryDetailDto()
                {
                    CustId = a.CustId,
                    SiteId = a.SiteId,
                    CurrencyId = a.CurrencyId,
                    BonusId = "VU" + a.Id,
                    BonusCode = WorkerType.ToString(),
                    BonusType = (int)BonusType.VipUpgrade,
                    UserName = a.UserName,
                    TransactionType = 0,
                    Amount = a.Amount,
                    Rollover = 0,
                    Turnover = 0,
                    ClaimDate = a.CreateTime
                });
            
                var result = await _promotionService.InsertReferralBonusSummary(referralBonus);
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