namespace HangfireService.Core.Models.DBs
{
    public class CustTicketInfoDto
    {
        public int SiteId { get; set; }

        public int CustId { get; set; }

        public int CurrencyId { get; set; }

        public int TicketType { get; set; }

        public byte TicketStatus { get; set; }
    }
}
