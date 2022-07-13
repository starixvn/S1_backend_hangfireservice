using Dapper;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;

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

        public Task<IEnumerable<TpSettlePendingModel>> GetTpSettlePendingPokerByTpGameType(int TpGameType, long BeginTime, long EndTime)
        {
            return _dbClient.QueryAsync<TpSettlePendingModel>("Service_TpSettlePending_Poker_Wager_Get", parameters: new DynamicParameters(new { TpGameType, BeginTime, EndTime }), commandTimeout: 60);
        }

        public Task<IEnumerable<TpWagerTimerModel>> GetTpWagerTimerList()
        {
            return _dbClient.QueryAsync<TpWagerTimerModel>("Service_TpWagerTimer_List_Get");
        }

        public Task<IEnumerable<TpWagerTimerModel>> GetTpWagerTimerReferenceList()
        {
            return _dbClient.QueryAsync<TpWagerTimerModel>("Service_TpWagerTimerReference_List_Get");
        }
    }
}
