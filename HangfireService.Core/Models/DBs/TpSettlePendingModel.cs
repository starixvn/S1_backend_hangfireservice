namespace HangfireService.Core.Models.DBs
{
    public class TpSettlePendingModel
    {
        public string? TransId { get; set; }

        public string? TpUserName { get; set; }
    }

    public class TpSettleDelayModel
    {
        public long SQ { get; set; }
        public int GameType { get; set; }
        public int TimeSecDiff { get; set; }
    }
}
