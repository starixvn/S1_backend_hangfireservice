namespace HangfireService.Core.Models.DBs
{
    public class CustWorldCupDepositDailySummaryDto
    {
        public int SiteId { get; set; }

        public int CurrencyId { get; set; }

        public int CustId { get; set; }

        public decimal DepositAmount { get; set; }

        public DateTime AccountDate { get; set; }

        /// <summary>
        /// 0:無效帳，1:有效帳
        /// </summary>
        public int Status { get; set; }
    }
}
