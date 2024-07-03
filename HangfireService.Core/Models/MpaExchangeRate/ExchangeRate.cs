namespace HangfireService.Core.Models.MpaExchangeRate;

public class ExchangeRate
{
    public string RateDate { get; set; }
    public int RateType { get; set; }
    public int CurrencyId { get; set; }
    public string CurrencyName { get; set; }
    public int TargetCurrencyId { get; set; }
    public string TargetCurrencyName { get; set; }
    public decimal Rate { get; set; }
}
