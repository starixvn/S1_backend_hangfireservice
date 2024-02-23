namespace HangfireService.Core.Models.DBs
{
    public class LeaderboardModel
    {
        public int SiteId { get; set; }
        public string SiteName { get; set; }
        public int CustId { get; set; }
        public string UserName { get; set; }
        public int CurrencyId { get; set; }
        public string IsoCode { get; set; }
        public int CategoryId { get; set; }
        public decimal Turnover { get; set; }
        public int WTurnover { get; set; }
    }

    public class Leaderboard
    {
        public int SiteId { get; set; }
        public string SiteName { get; set; }
        public int CurrencyId { get; set; }
        public string IsoCode { get; set; }
        public int CustId { get; set; }
        public string UserName { get; set; }
        public decimal WTurnover { get; set; }
    }

    public class LeaderboardPointCalculation
    {
        public string Date { get; set; }
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
        public decimal SportTurnover { get; set; }
        public decimal SlotTurnover { get; set; }
        public decimal LiveCasinoTurnover { get; set; }
    }
}
