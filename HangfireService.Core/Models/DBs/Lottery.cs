using HangfireService.Core.Models.Enums;

namespace HangfireService.Core.Models.DBs
{
    public class LotteryEvent
    {
        public int SiteId { get; set; }
        public int RoundId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime DrawDate { get; set; }
        public int Status { get; set; }
        public string ExcludeSetting { get; set; }
    }
    
    public class RaffleTicketSticker
    {
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
        public decimal StickerPrize { get; set; }
        public decimal TurnOver { get; set; }
    }
    
    public class LotteryTurnOverSummary
    {
        public int CustId { get; set; }
        public int CurrencyId { get; set; }
        public decimal ValidBet { get; set; }
    }

    public class CustLotteryTicket
    {
        public int CustId { get; set; }
        public int RoundId { get; set; }
        public string TicketNo { get; set; }
        public int StickerId { get; set; }
        public int Type { get; set; }
    }
    
}