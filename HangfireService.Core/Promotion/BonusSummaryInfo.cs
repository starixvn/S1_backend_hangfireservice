using HangfireService.Core.Models.Options;
using HangfireService.Core.Models.Promotion;

namespace HangfireService.Core.Promotion
{
    public class BonusSummaryInfo : PromotionSummaryBase
    {
        private readonly IPromotionService _promotionService;
        private readonly IBonusSummaryTimerService _timerService;

        public BonusSummaryInfo(IBonusSummaryTimerService timerService, IPromotionService promotionService, WorkerType type)
        {
            WorkerType = type;
            _promotionService = promotionService;
            _timerService = timerService;
        }

        public override async Task<bool> Run()
        {
            DateTime startTime;
            DateTime endTime;
            if (ManualOption != null)
            {
                startTime = ManualOption.StartTime;
                endTime = ManualOption.EndTime;
            }
            else
            {
                var timer = _timerService.GetBonusSummaryTimer((int)WorkerType);
                startTime = timer.StartTime.Date.AddDays(-1);
                endTime = DateTime.Now;
            }
            
            var dto = await _promotionService.GetBonusSummaryDetailByTime("Service_BonusSummaryDetail_Get", startTime, endTime);
            var referral = dto.ToList();
            if (referral.Any())
            {
                var bonusSummaryInfo = referral.Select(a => new InsertBonusSummaryInfoDto
                {
                    RecordDate = a.RecordDate,
                    SiteId = a.SiteId,
                    CurrencyId = a.CurrencyId,
                    BonusCode = a.BonusCode,
                    BonusType = a.BonusType,
                    BonusName = a.BonusName ?? "",
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    TotalClaimants = a.TotalClaimants,
                    TotalClaimedAmount = a.TotalClaimedAmount,
                    //todo
                    GGR = 0
                });
            
                var result = await _promotionService.InsertBonusSummaryInfo(bonusSummaryInfo);
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