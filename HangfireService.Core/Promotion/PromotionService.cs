using Dapper;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Promotion;
using Newtonsoft.Json;
using System.Data;

namespace HangfireService.Core.Promotion
{
    public class PromotionService : IPromotionService
    {
        private readonly MySqlClient _reportDb;
        private readonly MySqlClient _mainDb;

        public PromotionService(IEnumerable<MySqlClient> sqlClients)
        {
            _mainDb = sqlClients.FirstOrDefault(a => a.DataBase == DataBase.Main);
            _reportDb = sqlClients.FirstOrDefault(a => a.DataBase == DataBase.Report);
        }
        
        public async Task<IEnumerable<GetBonusSummaryDataByIdDto>> GetSummaryDataById(string sp, int id)
        {
            return await _reportDb.QueryAsync<GetBonusSummaryDataByIdDto>(sp, new { _Id = id });
        }

        public IEnumerable<GetBonusSummaryDataByIdDto> GetSummaryDataById(string sp, int id, out int maxCheckId)
        {
            maxCheckId = 0;
            var p = new DynamicParameters();
            p.Add("_Id", id);
            p.Add("_MaxCheckId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            var data = _reportDb.Query<GetBonusSummaryDataByIdDto>(sp, p);
            maxCheckId = p.Get<int?>("_MaxCheckId") ?? 0;
            return data;
        }

        public async Task<IEnumerable<GetBonusSummaryDataByTimeDto>> GetSummaryDataByTime(string sp, DateTime startTime, DateTime endTime)
        {
            return await _reportDb.QueryAsync<GetBonusSummaryDataByTimeDto>(sp, new { _StartTime = startTime, _EndTime = endTime });
        }
        
        public async Task<int> InsertBonusSummaryDetail(IEnumerable<InsertBonusSummaryDetailDto> bonusSummary)
        {
            return await _mainDb.ExecuteAsync("Service_BonusSummaryDetail_Ins",
                new { _BonusSummary = JsonConvert.SerializeObject(bonusSummary) });
        }
        
    }
}