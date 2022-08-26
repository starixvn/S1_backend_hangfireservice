namespace HangfireService.Core.Models.Options
{
    public class TpBetRecordRelayOption
    {
        public int SubtractDay { get; set; }
        public int TimeoutSeconds { get; set; } = 300;

        public DateTime BeginDate
        {
            get
            {
                return _beginDate ?? DateTime.UtcNow.Date.AddDays(SubtractDay - 1);
            }
            set
            {
                _beginDate = value;
            }
        }

        public DateTime EndDate
        {
            get
            {
                return _endDate ?? DateTime.UtcNow.Date.AddDays(-1);
            }
            set
            {
                _endDate = value;
            }
        }

        private DateTime? _beginDate { get; set; } = null;

        private DateTime? _endDate { get; set; } = null;
    }
}
