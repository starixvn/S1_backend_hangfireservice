namespace HangfireService.Core.Models.DBs
{
    public class TpCurrency
    {
        public int CurrencyId { set; get; }
        public string TpCurrencyId { set; get; }
        public decimal ExchangeRate { set; get; }
        public int IsSubCurrency { set; get; }
    }
    
    public class MpaRateDto
    {
        public DateTime RateDate { get; set; }

        public int CurrencyId { get; set; }

        public string CurrencyName { get; set; }

        public int TargetCurrencyId { get; set; }

        public string TargetCurrencyName { get; set; }

        public decimal Rate { get; set; }
    }
}
