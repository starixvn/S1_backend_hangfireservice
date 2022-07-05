namespace HangfireService.Core.Models.DBs
{
    public class DepositAmountSettingDto
    {
        public int SiteId { get; set; }

        public int CurrencyId { get; set; }

        /// <summary>
        /// 累積存款標準
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// 票券類型 Ticket1:1;Ticket2:2;Ticket3:3
        /// </summary>
        public int TicketType { get; set; }
    }
}
