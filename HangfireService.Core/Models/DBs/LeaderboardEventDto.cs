
namespace HangfireService.Core.Models.DBs;

public class LeaderboardEventDto
{
    public string EventCode { get; set; }
    public int SiteId { get; set; }
    public string StartTime { get; set; }
    public string EndTime { get; set; }
    public string TimeZoom { get; set; }
    public string SPName { get; set; }
    public string ExtraPointSP { get; set; }
}
