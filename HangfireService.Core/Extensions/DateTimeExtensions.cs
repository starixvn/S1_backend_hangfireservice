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
    }
}
