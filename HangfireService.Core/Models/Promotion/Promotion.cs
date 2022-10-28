namespace HangfireService.Core.Models.Promotion
{
    public class GetBonusSummaryDataByIdDto
    {
        public int Id { get; set; }
        public int SiteId { get; set; }
        public int CustId { get; set; }
        public int CurrencyId { get; set; }
        public string UserName { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreateTime { get; set; }
    }
    public class GetBonusSummaryDataByTimeDto
    {
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
        public int ClaimStatus { get; set; }
        public DateTime ClaimDate { get; set; }
    }

    public class ManualOption
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Id { get; set; }
    }
    public class InsertBonusSummaryInfoDto
    {
        public DateTime RecordDate { get; set; }
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
        public string BonusCode { get; set; }
        public int BonusType { get; set; }
        public int ClaimStatus { get; set; }
        public string BonusName { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int TotalClaimants { get; set; }
        public decimal TotalClaimedAmount { get; set; }
        public decimal GGR { get; set; }
    }
}