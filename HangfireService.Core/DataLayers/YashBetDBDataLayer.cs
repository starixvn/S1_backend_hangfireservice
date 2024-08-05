using HangfireService.Core.Helpers;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;


namespace HangfireService.Core.DataLayers
{
    public class YashBetDBDataLayer
    {
        private readonly MySqlClient _mainDBClient;
        

        public YashBetDBDataLayer(IServiceProvider provider)
        {
            _mainDBClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            var configBll = provider.GetRequiredService<ConfigBLL>();
        }
        
        public async Task<int> InsertHotEventInfoToYashBetDB(HotEventInfo request)
        {
            var result = 0;
            const string yashBetCurrency = "[74]";
            
            using var conn = _mainDBClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            if (request.Currency != null)
            {
                request.Currency = yashBetCurrency;
            }

            result = await _mainDBClient.ExecuteAsync("Service_HotEventInfo_Ins", new
            {
                _SiteId = 6,
                _TpId = request.TpId,
                _GameId = request.GameId,
                _SportLeaguesTitleId = request.SportLeaguesTitleId ,
                _MatchId = request.MatchId ,
                _StartTime = request.StartTime,
                _EndTime = request.EndTime,
                _BattleDateTime = request.BattleDateTime,
                _HomeSportLeaguesId = request.HomeSportLeaguesId,
                _AwaySportLeaguesId = request.AwaySportLeaguesId,
                _HomeSportTeamId = request.HomeSportTeamId,
                _AwaySportTeamId = request.AwaySportTeamId,
                _HomeOdds = request.HomeOdds,
                _AwayOdds = request.AwayOdds,
                _Handicap = request.Handicap ,
                _Sort = request.Sort,
                _Status = request.Status,
                _CreateUser = "JobSystem",
                _LastUpdateUser = "JobSystem",
                _IsLiveStreaming= request.IsLiveStreaming,
                _Currency = yashBetCurrency
            });
            
            trans.Commit();
            await conn.CloseAsync();
            conn.Dispose();
            
            return result;
        }
        
        public async Task<int> UpdateHotEventInfoToYashBetDB(HotEventInfo request)
        {
            var result = 0;
            const string yashBetCurrency = "[74]";
            
            using var conn = _mainDBClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            if (request.Currency != null)
            {
                request.Currency = yashBetCurrency;
            }

            result = await _mainDBClient.ExecuteAsync("Service_HotEventInfo_Set", new
            {
                _SQ = request.SQ,
                _SportLeaguesTitleId = request.SportLeaguesTitleId ,
                _MatchId = request.MatchId ,
                _StartTime = request.StartTime,
                _EndTime = request.EndTime,
                _BattleDateTime = request.BattleDateTime,
                _HomeSportLeaguesId = request.HomeSportLeaguesId,
                _AwaySportLeaguesId = request.AwaySportLeaguesId,
                _HomeSportTeamId = request.HomeSportTeamId,
                _AwaySportTeamId = request.AwaySportTeamId,
                _HomeOdds = request.HomeOdds,
                _AwayOdds = request.AwayOdds,
                _Handicap = request.Handicap ,
                _Sort = request.Sort,
                _Status = request.Status,
                _CreateUser = "JobSystem",
                _LastUpdateUser = "JobSystem",
                _IsLiveStreaming= request.IsLiveStreaming,
                _Currency = yashBetCurrency
            });
            
            trans.Commit();
            await conn.CloseAsync();
            conn.Dispose();
            
            return result;
        }
    }
}