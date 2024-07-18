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
        public int Type { get; set; }
    }
    
    public class LotteryEventDrawTurnOverSetting
    {
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
        public int Level { get; set; }
        public decimal TurnOverStart { get; set; }
        public decimal TurnOverEnd { get; set; }
    }
    
    public class LotteryDrawCount
    {
        public int DrawCount { get; set; }
        public int ExcludeCount { get; set; }
        public string Name { get; set; }
    }
    
    public class PrizeList
    {
        public int PrizeIndex { get; set; }
        public int PrizeId { get; set; }
        public int Tier { get; set; }
        public string PrizeName { get; set; }
        public int PrizeType { get; set; }
    }
    
    public class LotteryTicketHistory
    {
        public int RoundId { get; set; }
        public string FirstPrize { get; set; }
        public string SecondPrize { get; set; }
        public string ThirdPrize { get; set; }
        public string ConsolationPrize { get; set; }
        public string SmallPrize { get; set; }
        public int StickerId { get; set; }
    }
    
    public class EventRestriction
    {
        public int CurrencyId { get; set; }
        public int CustId { get; set; }
    }
}