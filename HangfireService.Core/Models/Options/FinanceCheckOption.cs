namespace HangfireService.Core.Models.Options
{
    public class FinanceCheckOption
    {
        public int SubtractDay { get; set; }

        public DateTime BeginTime
        {
            get
            {
                return _beginTime ?? DateTime.UtcNow.Date.AddDays(SubtractDay);
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
                return _endTime ?? DateTime.UtcNow.Date.AddDays(SubtractDay + 1);
            }
            set
            {
                _endTime = value;
            }
        }

        private DateTime? _beginTime { get; set; } = null;

        private DateTime? _endTime { get; set; } = null;
    }

    public class FinanceCheckTransIdOption
    {
        public int SubtractHour { get; set; }

        public DateTime BeginTime
        {
            get
            {
                var current = DateTime.UtcNow;
                var timeBase = new DateTime(
                    current.Year,
                    current.Month,
                    current.Day,
                    current.Hour,
                    0,
                    0
                );
                return _beginTime ?? timeBase.AddHours(SubtractHour);
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
                return _endTime ?? BeginTime.AddHours(1);
            }
            set
            {
                _endTime = value;
            }
        }

        private DateTime? _beginTime { get; set; } = null;

        private DateTime? _endTime { get; set; } = null;
    }
}
