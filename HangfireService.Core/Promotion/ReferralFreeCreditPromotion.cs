using HangfireService.Core.Models.Options;
using HangfireService.Core.Models.Promotion;

namespace HangfireService.Core.Promotion
{
    public class ReferralFreeCreditPromotion : PromotionSummaryBase
    {
        private readonly IPromotionService _promotionService;
        private readonly IBonusSummaryTimerService _timerService;

        public ReferralFreeCreditPromotion(IBonusSummaryTimerService timerService, IPromotionService promotionService, WorkerType type)
        {
            WorkerType = type;
            _promotionService = promotionService;
            _timerService = timerService;
        }

        public override async Task<bool> Run()
        {
            var timer = _timerService.GetBonusSummaryTimer((int)WorkerType);
            var startTime = timer.StartTime;
            var endTime = startTime.AddMinutes(15);
            
            var dto = await _promotionService.GetSummaryDataByTime("Service_ReferralFreeCreditSummary_Get", startTime, endTime);
            var referral = dto.ToList();
            if (referral.Any())
            {
                var referralBonus = referral.Select(a => new InsertBonusSummaryDetailDto()
                {
                    CustId = a.CustId,
                    SiteId = a.SiteId,
                    CurrencyId = a.CurrencyId,
                    BonusId = "RC" + a.CustId,
                    BonusCode = WorkerType.ToString(),
                    BonusType = (int)BonusType.ReferralFreeCredit,
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
                    _timerService.SetBonusSummaryTimer((int)WorkerType, endTime);
                    return true;
                }
            }
            _timerService.SetBonusSummaryTimer((int)WorkerType, endTime);
            return true;
        }
    }
}