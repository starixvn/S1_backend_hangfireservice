using HangfireService.Core.DataLayers;
using HangfireService.Core.Helpers;
using HangfireService.Core.Models.Enums;
using Newtonsoft.Json;

namespace HangfireService.Core.Service
{
    public class MpaUpdateMonthRateService
    {
        private readonly ReportDBDataLayer _reportDBDataLayer;
        private readonly MainDBDataLayer _mainDBDataLayer;
        private readonly ICacheHelper _redisHelper;
        private readonly string _allowedIpPartten = "AllowedIp";

        public MpaUpdateMonthRateService(IServiceProvider provider)
        {
            _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
            _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
        }

        public async Task<object> RunJobAsync(string reRunMonth = "")
        {
            bool isUpdateBefore = reRunMonth is "0";
            IEnumerable<string> monthList = isUpdateBefore ? new List<string>() : GetRunMonth(reRunMonth);
            var monthRateList = await _reportDBDataLayer.GetExchangeRate(isUpdateBefore, monthList);

            // insert to db
            await _mainDBDataLayer.InsertMonthlyExchangeRate(JsonConvert.SerializeObject(monthRateList));
            return true;
        }

        private IEnumerable<string> GetRunMonth(string reRunMonth = "")
        {
            var monthList = new List<string>();
            if (!string.IsNullOrEmpty(reRunMonth))
            {
                monthList.Add(reRunMonth);
            }
            else
            {
                monthList.Add(DateTime.Today.AddDays(1).ToString("yyyy-MM"));
                monthList.Add(DateTime.Today.ToString("yyyy-MM"));
                monthList.Add(DateTime.Today.AddMonths(-1).ToString("yyyy-MM"));
            }
            return monthList.Distinct();
        }

    }
}
