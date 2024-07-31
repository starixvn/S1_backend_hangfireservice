using HangfireService.Core.Helpers;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;


namespace HangfireService.Core.DataLayers
{
    public class YashBetDBDataLayer
    {
        private readonly MySqlClient _mainDBClient;
        private readonly MySqlClient _novaDBClient;
        private readonly string _reportDBConnectionString;

        public YashBetDBDataLayer(IServiceProvider provider)
        {
            _novaDBClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.ReportBackupDB);
            _mainDBClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            var configBll = provider.GetRequiredService<ConfigBLL>();
            _reportDBConnectionString = configBll.GetConfigValue<NovaDBClient>("HangfireHotEventInfo").Nova88ReportDB;
        }

        public class NovaDBClient
        {
            public string Nova88ReportDB { get; set; }
        }
        
        public async Task<IEnumerable<HotEventInfo>> GetHotEventInfoFromNova88(int siteId)
        {
            
            await using var conn = _novaDBClient.GetMySqlConnection();
            conn.Open();
            
            var result = await _novaDBClient.QueryWithConnectionAsync<HotEventInfo>("Service_HotEventInfo_Get", new
            {
                _SiteId = siteId
            },null,_reportDBConnectionString);
            
            await conn.CloseAsync();
            await conn.DisposeAsync();

            return result;

        }

        
        public async Task<int> SaveUpdatedHotEventInfoToYashBetDB(IEnumerable<HotEventInfo> request)
        {
            var result = 0;
            const string yashBetCurrency = "[74]";
            
            using var conn = _mainDBClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            foreach (var insert in request)
            {
                if (insert.Currency != null)
                {
                    insert.Currency = yashBetCurrency;
                }
    
                result = await _mainDBClient.ExecuteAsync("Admin_HotEventInfo_Ins_V3", new
                {
                    _SiteId = 6,
                    _TpId = insert.TpId,
                    _GameId = insert.GameId,
                    _SportLeaguesTitleId = insert.SportLeaguesTitleId ,
                    _MatchId = insert.MatchId ,
                    _StartTime = insert.StartTime,
                    _EndTime = insert.EndTime,
                    _BattleDateTime = insert.BattleDateTime,
                    _HomeSportLeaguesId = insert.HomeSportLeaguesId,
                    _AwaySportLeaguesId = insert.AwaySportLeaguesId,
                    _HomeSportTeamId = insert.HomeSportTeamId,
                    _AwaySportTeamId = insert.AwaySportTeamId,
                    _CreateUser = "JobSystem",
                    _LastUpdateUser = "JobSystem",
                    _IsLiveStreaming= insert.IsLiveStreaming,
                    _Currency = yashBetCurrency
                });
            }
            trans.Commit();
            await conn.CloseAsync();
            conn.Dispose();
            
            return result;
        }
    }
}