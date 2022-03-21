using Dapper;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace HangfireService.Core.DataLayers
{
    public class WagerDBDataLayer
    {
        private readonly MySqlClient _dbClient;

        public WagerDBDataLayer(IServiceProvider provider)
        {
            _dbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public Task<IEnumerable<TpSettlePendingModel>> GetTpSettlePendingLiveCasinoByTpGameType(int TpGameType, long BeginTime, long EndTime)
        {
            return _dbClient.QueryAsync<TpSettlePendingModel>("Service_TpSettlePending_LiveCasino_Wager_Get", parameters: new DynamicParameters(new { TpGameType, BeginTime, EndTime }), commandTimeout: 60);
        }
        public Task<IEnumerable<TpSettlePendingModel>> GetTpSettlePendingSlotByTpGameType(int TpGameType, long BeginTime, long EndTime)
        {
            return _dbClient.QueryAsync<TpSettlePendingModel>("Service_TpSettlePending_Slot_Wager_Get", parameters: new DynamicParameters(new { TpGameType, BeginTime, EndTime }), commandTimeout: 60);
        }
    }
}
