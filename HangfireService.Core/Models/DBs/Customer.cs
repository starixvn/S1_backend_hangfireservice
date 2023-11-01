namespace HangfireService.Core.Models.DBs
{
    /// <summary>
    /// 有需要其他欄位請參考Customer_Get SP自行擴充
    /// </summary>
    public class Customer
    {
        public string SiteName { get; set; }
        public int CustId { get; set; }
        public int SiteId { get; set; }
        public string UserName { get; set; }
        public int CurrencyId { get; set; }
    }
}
