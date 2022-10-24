using System.Collections.Immutable;
using System.Net.NetworkInformation;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Models.Promotion;

namespace HangfireService.Core.Promotion
{
    public class ReferralPromotion : PromotionSummaryBase
    {
        private readonly IReferralPromotionService _referralService;
        private readonly IBonusSummaryTimerService _timerService;

        public ReferralPromotion(IBonusSummaryTimerService timerService, IReferralPromotionService referralReferralService, WorkerType type)
        {
            WorkerType = type;
            _timerService = timerService;
            _referralService = referralReferralService;
        }
        public override async Task<bool> Run()
        {
            var id = await _timerService.GetWorkerId((int)WorkerType);
            var dto = await _referralService.GetReferralMLog(id);
            var referral = dto.ToList();
            if (referral.Any())
            {
                var referralBonus = referral.Select(a => new InsertBonusSummaryDetailDto()
                {
                    CustId = a.CustId,
                    SiteId = a.SiteId,
                    CurrencyId = a.CurrencyId,
                    BonusId = "RF" + a.Id,
                    BonusCode = "Referral",
                    BonusType = 10,
                    UserName = a.UserName,
                    TransactionType = 5,
                    Amount = a.Amount,
                    Rollover = 0,
                    Turnover = 0,
                    ClaimDate = a.CreateTime
                });
            
                var result = await _referralService.InsertReferralBonusSummary(referralBonus);
                if (result > 1)
                {
                    var lastId = referral.Max(a => a.Id);
                    await _timerService.SetWorkerId((int)WorkerType, "Referral", lastId);
                }
            }
            return true;
        }
    }
}