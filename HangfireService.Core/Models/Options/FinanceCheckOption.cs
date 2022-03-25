namespace HangfireService.Core.Models.Options
{
    public class FinanceCheckOption
    {
        private const int subtractDay = -1;

        public DateTime BeginTime 
        {
            get
            {
                return _beginTime ?? DateTime.Today.AddDays(subtractDay);
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
                return _endTime ?? DateTime.Today;
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
