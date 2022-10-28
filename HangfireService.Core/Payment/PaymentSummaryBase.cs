using HangfireService.Core.Models.Options;

namespace HangfireService.Core.Payment;

public abstract class PaymentSummaryBase
{
    public PaymentWorkerType WorkerType { get; set; }
    public abstract Task Run();
}