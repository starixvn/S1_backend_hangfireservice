namespace HangfireService.Core.Models.Promotion
{
    public class VipLevelLogDto
    {
        public int Id { get; set; }
        public int SiteId { get; set; }
        public int CustId { get; set; }
        public int CurrencyId { get; set; }
        public string UserName { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreateTime { get; set; }
    }

    public class InsertBonusSummaryDetailDto
    {
        public int CustId { get; set; }
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
        public string BonusId { get; set; }
        public string BonusCode { get; set; }
        public int BonusType { get; set; }
        public string UserName { get; set; }
        public int TransactionType { get; set; }
        public decimal Amount { get; set; }
        public decimal Rollover { get; set; }
        public decimal Turnover { get; set; }
        public DateTime ClaimDate { get; set; }
    }
    public class ReferralMLogDto
    {
        public int Id { get; set; }
        public int SiteId { get; set; }
        public int CustId { get; set; }
        public int CurrencyId { get; set; }
        public string UserName { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreateTime { get; set; }
    }
    public class ReferralLogDto
    {
        public int SiteId { get; set; }
        public int CustId { get; set; }
        public int CurrencyId { get; set; }
        public string UserName { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreateTime { get; set; }
    }
}