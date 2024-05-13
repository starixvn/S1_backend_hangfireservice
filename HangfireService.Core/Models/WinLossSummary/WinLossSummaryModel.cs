namespace HangfireService.Core.Models.WinLossSummary;

public class WinLossSummaryModel
{
    public WinLossSummaryModel(WinLossSourceModel source)
    {
        SettledTime = source.SettledTime;
        TransTime = source.TransTime;
        TpId = source.TpId;
        CategoryId = source.CategoryId;
        GameType = source.GameType;
        SiteId = source.SiteId;
        CurrencyId = source.CurrencyId;
        CustId = source.CustId;
        UserName = source.UserName;
        TpUserName = source.TpUserName;
        MemberType = source.MemberType;
        IsTest = source.IsTest;
        AffiliateName = source.AffiliateName;
        SettlementType = source.SettlementType;
        BonusId = source.BonusId;
        PromotionType = source.PromotionType;
        TicketStatus = source.TicketStatus;
        Stake = source.Stake;
        ValidBet = source.ValidBet;
        WinLoss = source.WinLoss;
        Revenue = source.Revenue;
        ProgressiveWin = source.ProgressiveWin;
        ProgressiveBet = source.ProgressiveBet;
        TotalCount = source.TotalCount;
        BaStake = source.BaStake;
        BaValidBet = source.BaValidBet;
        BaWinLoss = source.BaWinLoss;
        BaRevenue = source.BaRevenue;
        BaProgressiveWin = source.BaProgressiveWin;
        BaProgressiveBet = source.BaProgressiveBet;
    }

    public DateTime SettledTime { get; set; }
    public DateTime TransTime { get; set; }
    public int TpId { get; set; }
    public int CategoryId { get; set; }
    public int GameType { get; set; }
    public int SiteId { get; set; }
    public int CurrencyId { get; set; }
    public int CustId { get; set; }
    public string UserName { get; set; }
    public string TpUserName { get; set; }
    public int MemberType { get; set; }
    public int IsTest { get; set; }
    public string AffiliateName { get; set; }
    public int SettlementType { get; set; }
    public int BonusId { get; set; }
    public int PromotionType { get; set; }
    public int TicketStatus { get; set; }
    public decimal Stake { get; set; }
    public decimal ValidBet { get; set; }
    public decimal WinLoss { get; set; }
    public decimal Revenue { get; set; }
    public decimal ProgressiveWin { get; set; }
    public decimal ProgressiveBet { get; set; }
    public int TotalCount { get; set; }

    // SABA BA玩家使用
    public decimal BaStake { get; set; }
    public decimal BaValidBet { get; set; }
    public decimal BaWinLoss { get; set; }
    public decimal BaRevenue { get; set; }
    public decimal BaProgressiveWin { get; set; }
    public decimal BaProgressiveBet { get; set; }

    public decimal Ggr { get; set; }
}
