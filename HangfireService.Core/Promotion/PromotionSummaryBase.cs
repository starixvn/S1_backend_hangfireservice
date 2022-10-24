using HangfireService.Core.Models.Options;

namespace HangfireService.Core.Promotion
{
    public abstract class PromotionSummaryBase
    {
        public PromotionType PromotionType { get; set; }
        public abstract Task Run();
    }
    
}