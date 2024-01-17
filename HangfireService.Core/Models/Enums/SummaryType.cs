namespace HangfireService.Core.Models.Enums
{
    public enum SummaryType
    {
        TpBetRecord = 1,//下注紀錄
        TpRewardTotalAmount = 2,//活動總金額
        TpRewardFreeGame = 3,//活動免費遊戲
        TpRewardCash = 4,//活動現金獎勵
    }

    public enum TpCategoryEnum
    {
        Sports = 1,
        Casino = 2,
        LiveCasino = 3,
        Fishing = 4,
        Cockfighting = 5,
        Lottery = 6,
        VirtualSports = 7,
        Poker = 8,
        ESports = 11
    }
}
