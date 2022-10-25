using HangfireService.Core.Models.Options;
using HangfireService.Core.Models.Promotion;

namespace HangfireService.Core.Promotion
{
    public abstract class PromotionSummaryBase
    {
        public ManualOption ManualOption { get; set; }
        public WorkerType WorkerType { get; set; }
        public abstract Task<bool> Run();
    }
    
}