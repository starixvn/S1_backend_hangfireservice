using HangfireService.Core.Models.Enums;

namespace HangfireService.Core.Models.Options
{
    public class LeaderboardOption
    {
        public ExecuteType ExecuteType { get; set; } = ExecuteType.Months;
        public IEnumerable<int> CategoryList { get; set; }
        public IEnumerable<int> CurrencyList { get; set; }
        public int SiteId { get; set; }
        public int TimeoutSeconds { get; set; } = 300;
        public IEnumerable<int> beforeDays { get; set; }
        public string EventCode { get; set; }
        public int TopNum { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
}
