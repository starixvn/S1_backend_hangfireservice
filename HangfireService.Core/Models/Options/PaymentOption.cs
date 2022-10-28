namespace HangfireService.Core.Models.Options;

public class PaymentOption
{
    public PaymentWorkerType WorkerType { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? Id { get; set; }
}