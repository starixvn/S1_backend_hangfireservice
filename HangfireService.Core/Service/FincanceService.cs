using HangfireService.Core.DataLayers;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Finance;

namespace HangfireService.Core.Service
{
    public class FincanceService
    {
        private readonly MySqlClient _mainDbClient;


        public FincanceService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);

        }

        public IEnumerable<CheckData> GetTpDailySummary(int SummaryType, int TpId, DateTime BeginTime, DateTime EndTime)
        {
            return _mainDbClient.Query<CheckData>("Finance_Tp_DailySummary_Get", new { SummaryType, TpId, BeginTime, EndTime });
        }
    }
}
