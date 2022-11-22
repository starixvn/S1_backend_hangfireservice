namespace HangfireService.Core.Models.Enums
{
    public enum FinanceCheckType
    {
        結算資料與交易資料檢查,
        結算資料與第三方匯總檢查,
        交易資料與第三方匯總檢查,
    }

    public enum TicketStatusExcludeOption
    {
        Cancel = 0, // TicketStatus NOT IN (3)
        CancelAndUnsettle = 1, // TicketStatus NOT IN (0,3)
        Promo = 2 // TicketStatus NOT IN (6)
    }

    public enum TimeOption
    {
        SettledTime = 0,
        TransTime = 1,
    }
}
