namespace HangfireService.Core.Models.WinLossSummary;

public class WinLoss9WCommissionSourceModel
{
    public int CustId { get; set; }

    public DateTime TransTime { get; set; }

    public DateTime SettledTime { get; set; }

    public decimal Commission { get; set; }
}
