using HangfireService.Core.Models.Enums;

namespace HangfireService.Core.Models.DBs
{
    public class LotteryEvent
    {
        public int SiteId { get; set; }
        public int RoundId { get; set; }
        private DateTimeOffset _startDate { get; set; }
        public DateTimeOffset StartDate
        {
            get => _startDate;
            set => _startDate = new DateTimeOffset(value.DateTime, new TimeSpan(8, 0, 0));
        }
        private DateTimeOffset _endDate { get; set; }
        public DateTimeOffset EndDate
        {
            get => _endDate;
            set => _endDate = new DateTimeOffset(value.DateTime, new TimeSpan(8, 0, 0));
        }
        private DateTimeOffset _drawDate { get; set; }
        public DateTimeOffset DrawDate
        {
            get => _drawDate;
            set => _drawDate = new DateTimeOffset(value.DateTime, new TimeSpan(8, 0, 0));
        }
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
        public string UserName { get; set; }
        public int CurrencyId { get; set; }
        public int RoundId { get; set; }
        public int StickerId { get; set; }
        public decimal StickerAmount { get; set; }
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
    }
    
    public class Prize
    {
        public int PrizeId { get; set; }
        public int EventId { get; set; }
        public int CurrencyId { get; set; }
        public int PrizeType { get; set; }
        public int Jackpot { get; set; }
        public string PrizeName { get; set; }
        public decimal Amount { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Quota { get; set; }
        public int Rate { get; set; }
        public int Tier { get; set; }
        public DrawPrizeTypeEnum DrawPrizeType { get; set; }
    }
    
    public class LotteryTicketHistory
    {
        public int SiteId { get; set; }
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
    
    public class WinnerPayoutData
    {
        public string TicketNo { get; set; }
        public LotteryPrizeCategory LotteryPrizeCategory { get; set; }
        public string Name { get; set; }
    }
    
    public class DrawPrizeJackpotAccumulation
    {
        public int EventId { get; set; }
        public int PrizeId { get; set; }
        public int JackpotAmount { get; set; }
        public string PrizeName { get; set; }
    }
    
    
}