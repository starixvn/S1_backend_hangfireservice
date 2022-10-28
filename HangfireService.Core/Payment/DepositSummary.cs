using HangfireService.Core.Models.Options;
using HangfireService.Core.Models.Payment;
using HangfireService.Core.Models.Promotion;

namespace HangfireService.Core.Payment;

public class DepositSummary : PaymentSummaryBase
{
    private readonly IDepositSummaryService _depositService;
    private readonly IDailyPaymentSummaryTimerService _timerService;

    public DepositSummary(IDepositSummaryService depositService, IDailyPaymentSummaryTimerService timerService, PaymentWorkerType type)
    {
        WorkerType = type;
        _depositService = depositService;
        _timerService = timerService;
    }

    public override async Task Run()
    {
        var id = await _timerService.GetWorkerId((int)WorkerType);
        var data = await _depositService.GetDepositInfoSummary(id);
        var depositInfoSummary = data.ToList();
        if (depositInfoSummary.Any())
        {
            var dailyDeposit = depositInfoSummary.Select(a => new SetDailyDepositSummaryDto()
            {
                RecordDate = a.CreateTime,
                CustId = a.CustId,
                SiteId = a.SiteId,
                CurrencyId = a.CurrencyId,
                UserName = a.UserName,
                Amount = a.ActualDeposit,
            });
            
            var result = await _depositService.SetDailyDepositSummary(dailyDeposit);
            if (result > 1)
            {
                var lastId = depositInfoSummary.Max(a => a.Id);
                await _timerService.SetWorkerId((int)WorkerType, lastId);
            }
        }
    }
}