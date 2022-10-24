using System.Data;
using Dapper;
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

        public async Task<int> GetWorkerId(int type)
        {
            return await _reportDb.QueryFirstAsync<int>("Service_BonusSummaryTimerId_Get", new { _Type = type });
        }

        public async Task<int> SetWorkerId(int type, string memo, int id)
        {
            return await _mainDb.ExecuteAsync("Service_BonusSummaryTimerId_Set", new { _Type = type, _Memo = memo, _Id = id });
        }

        public BonusSummaryTimer GetBonusSummaryTimer(int type)
        {
            return _reportDb.QueryFirst<BonusSummaryTimer>("Service_BonusSummaryTimer_Get", new { _Type = type });
        }

        public int SetBonusSummaryTimer(int type, DateTime time)
        {
            return _mainDb.Execute("Service_BonusSummaryTimer_Set" , new { _Type = type , _Time = time});
        }
    }
}