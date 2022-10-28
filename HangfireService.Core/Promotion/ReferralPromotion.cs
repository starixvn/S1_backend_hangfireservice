using System.Collections.Immutable;
using System.Net.NetworkInformation;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Models.Promotion;
using Microsoft.Extensions.Logging;

namespace HangfireService.Core.Promotion
{
    public class ReferralPromotion : PromotionSummaryBase
    {
        private readonly IPromotionService _promotionService;
        private readonly IBonusSummaryTimerService _timerService;
        private readonly ILogger<ReferralPromotion> _logger;
        public ReferralPromotion(IBonusSummaryTimerService timerService, IPromotionService promotionService, WorkerType type, ILogger<ReferralPromotion> logger)
        {
            WorkerType = type;
            _timerService = timerService;
            _promotionService = promotionService;
            _logger = logger;
        }
        public override async Task<bool> Run()
        {
            int id;
            if (ManualOption != null) 
                id = ManualOption.Id;
            else 
                id = await _timerService.GetWorkerId((int)WorkerType);
            
            var dto = await _promotionService.GetSummaryDataById("Service_ReferralSummary_Get", id);
            var referral = dto.ToList();
            if (referral.Any())
            {
                var bonusSummaryDetail = referral.Select(a => new InsertBonusSummaryDetailDto()
                {
                    CustId = a.CustId,
                    SiteId = a.SiteId,
                    CurrencyId = a.CurrencyId,
                    BonusId = "RF" + a.Id,
                    BonusCode = WorkerType.ToString(),
                    BonusType = (int)BonusType.Referral,
                    UserName = a.UserName,
                    TransactionType = 0,
                    Amount = a.Amount,
                    Rollover = 0,
                    Turnover = 0,
                    ClaimDate = a.CreateTime
                });
            
                var result = await _promotionService.InsertBonusSummaryDetail(bonusSummaryDetail);
                if (result > 1 && ManualOption == null)
                {
                    var lastId = referral.Max(a => a.Id);
                    await _timerService.SetWorkerId((int)WorkerType, "ReferralMLog", lastId);
                }
            }
            return true;
        }
    }
}