namespace HangfireService.Core.Models.Enums
{
    public enum LotteryTicketEnum
    {
        Ticket = 1,
        Sticker = 2
    }
    
    public enum DrawPrizeTypeEnum
    {
        NormalPrize = 0,
        NoPrize = 1,
        BlessingsSticker = 2
    }

    public enum LotteryEventDrawEnum
    {
        Running = 0,
        Done = 1,
        Suspend = 2,
        PayOut = 3
    }
    
    public enum LotteryPrizeCategory
    {
        FirstPrize = 1,
        SecondPrize = 2,
        ThirdPrize = 3,
        ConsolationPrize = 4,
        SmallPrize = 5
    }
}
