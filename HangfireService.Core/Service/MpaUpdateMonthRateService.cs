using HangfireService.Core.DataLayers;
using Newtonsoft.Json;

namespace HangfireService.Core.Service
{
    public class MpaUpdateMonthRateService
    {
        private readonly ReportDBDataLayer _reportDBDataLayer;
        private readonly MainDBDataLayer _mainDBDataLayer;

        public MpaUpdateMonthRateService(IServiceProvider provider)
        {
            _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
            _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
        }

        public async Task<object> RunJobAsync(bool isMamualRun, string reRunMonth = "")
        {
            bool isUpdateBefore = reRunMonth is "0";
            var Date = isUpdateBefore ? (StartDate: "2000-01-01", EndDate: "2050-12-31") : GetStartAndEndDate(isMamualRun, reRunMonth);
            var monthRateList = await _reportDBDataLayer.GetExchangeRate(isUpdateBefore, Date.StartDate, Date.EndDate);

            // insert to db
            await _mainDBDataLayer.InsertMonthlyExchangeRate(JsonConvert.SerializeObject(monthRateList));
            return true;
        }

        private (string StartDate, string EndDate) GetStartAndEndDate(bool isMamualRun, string reRunMonth = "")
        {
            if (string.IsNullOrWhiteSpace(reRunMonth))
            {
                reRunMonth = DateTime.Today.ToString("yyyy-MM-dd");
            }

            // 解析傳入的年月
            var parts = reRunMonth.Split('-');
            int year = int.Parse(parts[0]);
            int month = int.Parse(parts[1]);

            // 計算 StartTime
            int startMonth = isMamualRun ? month : month - 1;
            int startYear = year;
            if (startMonth == 0)
            {
                startMonth = 12;
                startYear -= 1;
            }
            var startTime = new DateTime(startYear, startMonth, 1).ToString("yyyy-MM-dd");

            // 計算 EndTime
            int endDay = DateTime.DaysInMonth(year, month);
            var endTime = new DateTime(year, month, endDay).ToString("yyyy-MM-dd");

            return (StartDate: startTime, EndDate: endTime);

        }

    }
}
