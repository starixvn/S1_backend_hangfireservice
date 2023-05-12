using HangfireService.Core.Extensions;

namespace HangfireService.Core.Models.Options
{
    public class LeaderboardOption
    {
        public int CategoryId { get; set; }
        public string EventCode { get; set; }
        public int SiteId { get; set; }
        public int TopNum { get; set; }
        public int TimeoutSeconds { get; set; } = 300;
        public IEnumerable<int> CurrencyList { get; set; }

        public DateTime StartTime
        {
            get
            {
                return _beginTime ?? GetMonthRange().StartTime;
            }
            set
            {
                _beginTime = value;
            }
        }

        public DateTime EndTime
        {
            get
            {
                return _endTime ?? GetMonthRange().EndTime;
            }
            set
            {
                _endTime = value;
            }
        }

        private DateTime? _beginTime { get; set; } = null;

        private DateTime? _endTime { get; set; } = null;

        private (DateTime StartTime, DateTime EndTime) GetMonthRange()
        {
            var now = DateTime.UtcNow;
            var start = new DateTime(now.Year, now.Month, 1);
            var end = start.GetMonthEnd();
            return (start, end);
        }
    }
}
