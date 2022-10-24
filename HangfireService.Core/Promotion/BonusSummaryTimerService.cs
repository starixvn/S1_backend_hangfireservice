using Dapper;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;

namespace HangfireService.Core.Promotion
{
    public class BonusSummaryTimerService : IBonusSummaryTimerService
    {
        private readonly MySqlClient _sqlClient;

        public BonusSummaryTimerService(IEnumerable<MySqlClient> sqlClients)
        {
            _sqlClient = sqlClients.FirstOrDefault(a => a.DataBase == DataBase.Report);
        }

        public async Task<int> GetWorkerId(int type)
        {
            return await _sqlClient.QueryFirstAsync<int>("Service_BonusSummaryTimerId_Get", new { _Type = type });
        }

        public async Task<int> SetWorkerId(int type, string memo, int id)
        {
            return await _sqlClient.ExecuteAsync("Service_BonusSummaryTimerId_Set", new { _Type = type, _Memo = memo, _Id = id });
        }

        public BonusSummaryTimer GetBonusSummaryTimer(int type)
        {
            return _sqlClient.QueryFirst<BonusSummaryTimer>("Service_BonusSummaryTimer_Get", new { _Type = type });
        }
    }
}