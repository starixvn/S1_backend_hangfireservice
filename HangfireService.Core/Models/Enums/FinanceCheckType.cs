using System.ComponentModel;
using HangfireService.Core.Attributes;

namespace HangfireService.Core.Models.Enums
{
    public enum FinanceCheckType
    {
        [Description("Compare between sum of TpBetRecord and Sum of Bettrans")]
        CompareTpBetRecordSumAndBettrans,
        [Description("Compare between Daily Summary from Third Party and sum of TpBetRecord")]
        CompareTpDailySummaryAndTpBetRecordSum,
        [Description("Compare between sum of Bettrans and Daily Summary from Third Party")]
        CompareBettransAndTpDailySummary,
    }

    public enum FinanceCheckTransIdType
    {
        結算與交易資料檢查,
        結算與交易單號檢查,
        交易與結算單號檢查,
        結算與交易金額檢查,
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
        TransId = 2,
    }

    public enum ConditionOption
    {
        Time = 0,
        TransId = 1,
    }
}
