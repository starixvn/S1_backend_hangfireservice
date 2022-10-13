namespace HangfireService.Core.Models.DBs;

public class TpBettingCheckModel
{
    public int SiteId { set; get; }
    public string TpName { set; get; }
    public int TpId { set; get; }
    public int GameType { set; get; }
    public int CurrencyId { set; get; }
    public string Currency { set; get; }
    public int Frequency { set; get; }
}

public class TpLastBetting
{
    public DateTime LastBettingTime { set; get; }
}