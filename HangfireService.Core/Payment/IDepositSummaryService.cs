using HangfireService.Core.Models.Payment;

namespace HangfireService.Core.Payment;

public interface IDepositSummaryService
{
    Task<IEnumerable<GetDepositLogummaryDto>> GetDepositInfoSummary(int id);
    Task<int> SetDailyDepositSummary(IEnumerable<SetDailyDepositSummaryDto> depositSummary);
}