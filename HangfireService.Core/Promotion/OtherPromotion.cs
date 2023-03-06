using HangfireService.Core.Models.Options;
using HangfireService.Core.Models.Promotion;
using Microsoft.Extensions.Logging;

namespace HangfireService.Core.Promotion
{
    public class OtherPromotion : PromotionSummaryBase
    {
        private readonly IPromotionService _promotionService;
        private readonly IBonusSummaryTimerService _timerService;
        private readonly ILogger<OtherPromotion> _logger;

        public OtherPromotion(IBonusSummaryTimerService timerService, IPromotionService promotionService, WorkerType type, ILogger<OtherPromotion> logger)
        {
            WorkerType = type;
            _timerService = timerService;
            _promotionService = promotionService;
            _logger = logger;
        }

        public override async Task<bool> Run()
        {
            try
            {
                int id;
                if (ManualOption != null) 
                    id = ManualOption.Id;
                else 
                    id = _timerService.GetBonusSummaryTimer((int)WorkerType).Id;

                var dto = _promotionService.GetSummaryDataById("Service_OtherSummary_Get", id, out var maxCheckId);
                var adjustmentInfo = dto.ToList();
            
                if (adjustmentInfo.Any())
                {
                    var bonusSummaryDetail = adjustmentInfo.Select(a => new InsertBonusSummaryDetailDto()
                    {
                        CustId = a.CustId,
                        SiteId = a.SiteId,
                        CurrencyId = a.CurrencyId,
                        BonusId = "AJ" + a.Id,
                        BonusCode = WorkerType.ToString(),
                        BonusType = (int)BonusType.Other,
                        UserName = a.UserName,
                        TransactionType = 0,
                        Amount = a.Amount,
                        Rollover = 0,
                        Turnover = 0,
                        ClaimDate = a.CreateTime
                    });
            
                    var result = await _promotionService.InsertBonusSummaryDetail(bonusSummaryDetail);
                }
                if (maxCheckId > 1 && ManualOption == null)
                {
                    await _timerService.SetWorkerTimer((int)WorkerType, "AdjustmentInfo", maxCheckId, null);
                }
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return false;
            }
        }
    }
}

