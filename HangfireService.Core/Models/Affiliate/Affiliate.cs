namespace HangfireService.Core.Models.Affiliate
{
    public class AffiliateInfo
    {
        public int CustId { get; set; }
        public int SiteId { get; set; }
        public string AffiliateName { get; set; }
        public string ParentAffiliate { get; set; }
        public string AffiliateCode { get; set; }
        public string UserName { get; set; }
    }

    public class AffiliatePara
    {
        public List<SiteData> SiteList { get; set; }
    }
    
    public class SiteData
    {
        public int SiteId { get; set; }
        public string SiteShortName { get; set; }
    }
}
