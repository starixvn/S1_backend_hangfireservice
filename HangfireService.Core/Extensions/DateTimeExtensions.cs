namespace HangfireService.Core.Extensions
{
    public static class DateTimeExtensions
    {
        public static bool IsBetween(this DateTime dt, DateTime startDate, DateTime endDate, bool compareTime = true)
        {
            return compareTime ?
               dt >= startDate && dt <= endDate :
               dt.Date >= startDate.Date && dt.Date <= endDate.Date;
        }

        public static long GetUnixTimeByDay(this DateTime datetime)
        {
            var time = new DateTime(
                datetime.Year,
                datetime.Month,
                datetime.Day,
                0,
                0,
                0
            );
            return new DateTimeOffset(time).ToUnixTimeSeconds();
        }

        public static string GetPartitionKey(this DateTime partitionTime)
        {
            return partitionTime.ToString("yyyyMM");
        }

        public static IEnumerable<string> GetPartitionRange(this DateTime beginTime, DateTime EndTime)
        {
            var result = new List<string>
            {
                beginTime.GetPartitionKey()
            };
            var date = beginTime;
            while (date.GetPartitionKey() != EndTime.GetPartitionKey())
            {
                date = date.AddMonths(1);
                result.Add(date.GetPartitionKey());
            }
            return result;
        }


        /// <param name="timeZone">format:+08:00</param>
        public static DateTimeOffset ToOffset(this DateTime time, string timeZone)
        {
            return DateTimeOffset.Parse(time.ToString() + timeZone);
        }
    }
}
