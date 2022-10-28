namespace HangfireService.Core.Models.Payment;

public class GetDepositLogummaryDto
{
    public int Id { get; set; }
    public int SiteId { get; set; }
    public int CurrencyId { get; set; }
    public int CustId { get; set; }
    public string UserName { get; set; }
    public decimal ActualDeposit { get; set; }
    public DateTime CreateTime { get; set; }
}

public class SetDailyDepositSummaryDto
{
    public DateTime RecordDate { get; set; }
    public int SiteId { get; set; }
    public int CurrencyId { get; set; }
    public int CustId { get; set; }
    public string UserName { get; set; }
    public decimal Amount { get; set; }
    public decimal ActualAmount { get; set; }
}