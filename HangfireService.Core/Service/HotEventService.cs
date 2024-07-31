using HangfireService.Core.DataLayers;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;

namespace HangfireService.Core.Service
{
    public class HotEventService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly YashBetDBDataLayer _yashBetDBDataLayer;
        
        public HotEventService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _yashBetDBDataLayer = provider.GetRequiredService<YashBetDBDataLayer>();
        }
        public async Task<int> HotEventInfoJobAsync()
        {
            
            var nowTime = DateTime.Now;
             
            var nova88HotEventData = await GetHotEventInfoFromNova88(1);
            var yashBetHotEventData = await GetHotEventInfoFromYashBet(6);
            
            var yashBetHotEventDataDict = yashBetHotEventData
                .GroupBy(x => new { x.SiteId, x.TpId, x.GameId, x.SportLeaguesTitleId, x.MatchId })
                .ToDictionary(g => g.Key, g => g.First());
            
            var yashBetHotEventDataList = yashBetHotEventData.ToList();

            foreach (var nova88Event in nova88HotEventData)
            {
                var key = new { nova88Event.SiteId, nova88Event.TpId, nova88Event.GameId, nova88Event.SportLeaguesTitleId, nova88Event.MatchId };
                if (!yashBetHotEventDataDict.ContainsKey(key))
                {
                    yashBetHotEventDataList.Add(nova88Event);
                }
                else if (nova88Event.LastUpdateTime > nowTime.AddHours(-3))
                {
                    var yashBetEventData = yashBetHotEventDataDict[key];
                    yashBetEventData.StartTime = nova88Event.StartTime;
                    yashBetEventData.EndTime = nova88Event.EndTime;
                    yashBetEventData.BattleDateTime = nova88Event.BattleDateTime;
                    yashBetEventData.HomeSportLeaguesId = nova88Event.HomeSportLeaguesId;
                    yashBetEventData.AwaySportLeaguesId = nova88Event.AwaySportLeaguesId;
                    yashBetEventData.HomeSportTeamId = nova88Event.HomeSportTeamId;
                    yashBetEventData.AwaySportTeamId = nova88Event.AwaySportTeamId;
                    yashBetEventData.HomeOdds = nova88Event.HomeOdds;
                    yashBetEventData.AwayOdds = nova88Event.AwayOdds;
                    yashBetEventData.Handicap = nova88Event.Handicap;
                    yashBetEventData.Sort = nova88Event.Sort;
                    yashBetEventData.Status = nova88Event.Status;
                    yashBetEventData.CreateTime = nova88Event.CreateTime;
                    yashBetEventData.CreateUser = nova88Event.CreateUser;
                    yashBetEventData.LastUpdateTime = nova88Event.LastUpdateTime;
                    yashBetEventData.LastUpdateUser = nova88Event.LastUpdateUser;
                    yashBetEventData.IsLiveStreaming = nova88Event.IsLiveStreaming;
                    yashBetEventData.Currency = nova88Event.Currency;
                }
            }
            
            var result = await SaveUpdatedHotEventInfoToYashBetDB(yashBetHotEventDataList);
            
            return result;
        }
        
        #region Private
        
        private async Task<IEnumerable<HotEventInfo>> GetHotEventInfoFromNova88(int siteId)
        {
            var result = await _yashBetDBDataLayer.GetHotEventInfoFromNova88(siteId);
            return result;
        }
        
        private async Task<IEnumerable<HotEventInfo>> GetHotEventInfoFromYashBet(int siteId)
        {
            var result = await _mainDbClient.QueryAsync<HotEventInfo>("Service_HotEventInfo_Get", new
            {
                _SiteId = siteId
            });
            
            return result;
        }
        
        private async Task<int> SaveUpdatedHotEventInfoToYashBetDB(IEnumerable<HotEventInfo> hotEventData)
        {
            var result = await _yashBetDBDataLayer.SaveUpdatedHotEventInfoToYashBetDB(hotEventData);
            return result;
        }
        
        #endregion 

    }
}