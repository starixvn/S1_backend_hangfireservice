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
            int id;
            if (ManualOption != null) 
                id = ManualOption.Id;
            else 
                id = await _timerService.GetWorkerId((int)WorkerType);

            var dto = await _promotionService.GetSummaryDataById("Service_ReferralFreeCreditSummary_Get", id);
            var referralFreeCredit = dto.ToList();
            if (referralFreeCredit.Any())
            {
                var referralBonus = referralFreeCredit.Select(a => new InsertBonusSummaryDetailDto()
                {
                    CustId = a.CustId,
                    SiteId = a.SiteId,
                    CurrencyId = a.CurrencyId,
                    BonusId = "RC" + a.Id,
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
                if (result > 1 && ManualOption == null)
                {
                    var lastId = referralFreeCredit.Max(a => a.Id);
                    await _timerService.SetWorkerId((int)WorkerType, "ReferralLog", lastId);
                }
            }
            return true;
        }
    }
}