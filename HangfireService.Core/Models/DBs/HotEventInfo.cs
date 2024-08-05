namespace HangfireService.Core.Models.DBs
{
    public class HotEventInfoConfig
    {
        public string ApiUrl { get; set; }
    }

    public class MemberSiteHotEventInfoResponse
    {
        public int errorCode { get; set; }
        public string message { get; set; }
        public string rawErrorCode { get; set; }
        public string rawMessage { get; set; }
        public IEnumerable<HotEventInfo> data { get; set; }
    }
    
    public class HotEventInfo
    {
        public int SQ { get; set; }
        public int SiteId { get; set; }
        public int TpId { get; set; }
        public int GameId { get; set; }
        public int SportLeaguesTitleId { get; set; }
        public int MatchId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime BattleDateTime { get; set; }
        public int HomeSportLeaguesId { get; set; }
        public int AwaySportLeaguesId { get; set; }
        public int HomeSportTeamId { get; set; }
        public int AwaySportTeamId { get; set; }
        public decimal HomeOdds { get; set; }
        public decimal AwayOdds { get; set; }
        public decimal Handicap { get; set; }
        public int Sort { get; set; }
        public int Status { get; set; }
        public DateTime CreateTime { get; set; }
        public string CreateUser { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public string LastUpdateUser { get; set; }
        public int IsLiveStreaming { get; set; }
        public string Currency { get; set; }
        public int DataType;
    }
}
