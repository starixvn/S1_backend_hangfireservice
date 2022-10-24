using HangfireService.Core.Models.Options;
using HangfireService.Core.Models.Promotion;

namespace HangfireService.Core.Promotion
{
    public class OtherPromotion : PromotionSummaryBase
    {
        private readonly IOtherPromotionService _otherService;
        private readonly IBonusSummaryTimerService _timerService;

        public OtherPromotion(IBonusSummaryTimerService timerService, IOtherPromotionService otherService, WorkerType type)
        {
            WorkerType = type;
            _timerService = timerService;
            _otherService = otherService;
        }

        public override async Task<bool> Run()
        {
            var id = await _timerService.GetWorkerId((int)WorkerType);
            var dto = await _otherService.GetAdjustmentInfo(id);
            var adjustmentInfo = dto.ToList();
            if (adjustmentInfo.Any())
            {
                var bonusSummaryDetail = adjustmentInfo.Select(a => new InsertBonusSummaryDetailDto()
                {
                    CustId = a.CustId,
                    SiteId = a.SiteId,
                    CurrencyId = a.CurrencyId,
                    BonusId = "AJ" + a.Id,
                    BonusCode = "Other",
                    BonusType = 13,
                    UserName = a.UserName,
                    TransactionType = 0,
                    Amount = a.Amount,
                    Rollover = 0,
                    Turnover = 0,
                    ClaimDate = a.CreateTime
                });
            
                var result = await _otherService.InsertReferralBonusSummary(bonusSummaryDetail);
                if (result > 1)
                {
                    var lastId = adjustmentInfo.Max(a => a.Id);
                    await _timerService.SetWorkerId((int)WorkerType, "AdjustmentInfo", lastId);
                }
            }
            return true;
        }
    }
}

