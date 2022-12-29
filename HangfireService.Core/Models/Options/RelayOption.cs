namespace HangfireService.Core.Models.Options
{
    public class RelayOption
    {
        public string GetSP { get; set; }
        public string InsertSP { get; set; }
        public string DeleteSP { get; set; }
        public ExecuteType ExecuteType { get; set; }
        public int Subtract { get; set; } = 0;
        public int TimeoutSeconds { get; set; } = 300;
        public int LiminNum { get; set; } = 2000;

        public string SummaryType { get; set; } = "";

        public DateTime StartTime
        {
            get
            {
                return _beginTime ?? GetTimeRange().StartTime;
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
                return _endTime ?? GetTimeRange().EndTime;
            }
            set
            {
                _endTime = value;
            }
        }

        private DateTime? _beginTime { get; set; } = null;

        private DateTime? _endTime { get; set; } = null;

        private (DateTime StartTime, DateTime EndTime) GetTimeRange()
        {
            if (ExecuteType == ExecuteType.Days)
            {
                return (DateTime.UtcNow.Date.AddDays(Subtract), DateTime.UtcNow.Date.AddDays(-1));
            }

            var now = DateTime.UtcNow;
            var lastHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);
            return (lastHour.AddHours(Subtract), lastHour);
        }
    }

    public enum ExecuteType
    {
        Days,
        Hours
    }
}
