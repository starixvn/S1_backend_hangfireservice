namespace HangfireService.Core.Models.Options
{
    public class AllArchiveOption
    {
        public int LiminNum { get; set; } = 10000;
        public int TaskCount { get; set; } = 8;
       
        /// <summary>
        /// DB離峰開始小時(+08:00)
        /// </summary>
        public string SafeRangeStartHour { get; set; } = "04:00";

        /// <summary>
        /// DB離峰結束小時(+08:00)
        /// </summary>
        public string SafeRangeEndHour { get; set; } = "16:00";

        /// <summary>
        /// DB離峰開始時間(+08:00)
        /// </summary>
        public DateTime SafeRangeStartTime 
        {
            get
            {
                var taipeiLocalDate = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8)).Date;
                var startHour = TimeSpan.Parse(SafeRangeStartHour);
                return taipeiLocalDate.Add(startHour);
            }
        }

        /// <summary>
        /// DB離峰結束時間(+08:00)
        /// </summary>
        public DateTime SafeRangeEndTime
        {
            get
            {
                var taipeiLocalDate = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8)).Date;
                var endHour = TimeSpan.Parse(SafeRangeEndHour);
                return taipeiLocalDate.Add(endHour);
            }
        }

        public List<ArchiveDetail> ArchiveList { get; set; }
    }

    public class ArchiveDetail
    {
        public string ArchiveName { get; set; } = string.Empty;
        public string GetSP { get; set; } = string.Empty;
        public string InsertSP { get; set; } = string.Empty;
        public string DeleteSP { get; set; } = string.Empty;
        public int ArchiveDays { get; set; } = 7;
        public string ArchiveType { get; set; }
    }
}
