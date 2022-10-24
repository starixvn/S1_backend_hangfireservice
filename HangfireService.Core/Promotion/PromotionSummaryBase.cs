using HangfireService.Core.Models.Options;

namespace HangfireService.Core.Promotion
{
    public abstract class PromotionSummaryBase
    {
        public WorkerType WorkerType { get; set; }
        public abstract Task<bool> Run();
    }
    
}