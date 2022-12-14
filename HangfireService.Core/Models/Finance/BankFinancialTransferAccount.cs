namespace HangfireService.Core.Models.Finance;

public class BankFinancialTransferAccount
{
    public int Id { get; set; }
    public int SiteId { get; set; }
    public int CurrencyId { get; set; }
    public string SiteName { get; set; }
    public string DisplayName { get; set; }
    public string IsoCode { get; set; }
}