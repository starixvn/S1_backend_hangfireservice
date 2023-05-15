namespace HangfireService.Core.Models.DBs
{
    public class TpCurrency
    {
        public int CurrencyId { set; get; }
        public string TpCurrencyId { set; get; }
        public decimal ExchangeRate { set; get; }
        public int IsSubCurrency { set; get; }
    }
}
