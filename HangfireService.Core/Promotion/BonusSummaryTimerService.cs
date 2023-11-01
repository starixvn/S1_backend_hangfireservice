using HangfireService.Core.DataLayers;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;

namespace HangfireService.Core.Promotion
{
    public class BonusSummaryTimerService : IBonusSummaryTimerService
    {
        private readonly MySqlClient _reportDb;
        private readonly MySqlClient _mainDb;

        public BonusSummaryTimerService(IEnumerable<MySqlClient> sqlClients)
        {
            _mainDb = sqlClients.FirstOrDefault(a => a.DataBase == DataBase.Main);
            _reportDb = sqlClients.FirstOrDefault(a => a.DataBase == DataBase.Report);
        }

        public async Task<int> SetWorkerTimer(int type, string memo, int id, DateTime? time)
        {
            return await _mainDb.ExecuteAsync("Service_Timer_Set", new { _Type = type, _Memo = memo, _Id = id, _StartTime = time });
        }

        public BonusSummaryTimer GetBonusSummaryTimer(int type)
        {
            return _reportDb.QueryFirst<BonusSummaryTimer>("Service_Timer_Get", new { _Type = type });
        }
    }
}