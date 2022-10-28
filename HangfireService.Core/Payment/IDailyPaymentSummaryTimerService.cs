namespace HangfireService.Core.Payment;

public interface IDailyPaymentSummaryTimerService
{
    Task<DateTime> GetWorkerTime(int type);
    Task<int> GetWorkerId(int type);
    Task<int> SetWorkerTime(int type, DateTime time);
    Task<int> SetWorkerId(int type, int id);
}