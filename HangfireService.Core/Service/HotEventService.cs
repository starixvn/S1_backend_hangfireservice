using HangfireService.Core.DataLayers;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using Microsoft.Extensions.Logging;

namespace HangfireService.Core.Service
{
    public class HotEventService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly ILogger<HotEventService> _logger;
        private readonly YashBetDBDataLayer _yashBetDBDataLayer;
        
        public HotEventService(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<HotEventService>>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _yashBetDBDataLayer = provider.GetRequiredService<YashBetDBDataLayer>();
        }
        public async Task<int> HotEventInfoJobAsync()
        {
            
            var result = 0;
            var nowTime = DateTime.Now;
             
            var nova88HotEventData = await GetHotEventInfoFromNova88(1);
            var yashBetHotEventData = await GetHotEventInfoFromYashBet(6);
            
            var yashBetHotEventDataDict = yashBetHotEventData
                .GroupBy(x => new
                {
                    x.TpId,
                    x.GameId,
                    x.SportLeaguesTitleId,
                    x.MatchId,
                    x.StartTime,
                    x.EndTime,
                    x.BattleDateTime,
                    x.HomeSportLeaguesId,
                    x.AwaySportLeaguesId,
                    x.HomeSportTeamId,
                    x.AwaySportTeamId,
                    x.HomeOdds,
                    x.AwayOdds,
                    x.Handicap,
                    x.IsLiveStreaming,
                })
                .ToDictionary(g => g.Key, g => g.First());
            
            //var yashBetHotEventDataList = yashBetHotEventData.ToList();

            foreach (var nova88Event in nova88HotEventData)
            {
                var key = new
                {
                    nova88Event.TpId,
                    nova88Event.GameId,
                    nova88Event.SportLeaguesTitleId,
                    nova88Event.MatchId,
                    nova88Event.StartTime,
                    nova88Event.EndTime,
                    nova88Event.BattleDateTime,
                    nova88Event.HomeSportLeaguesId,
                    nova88Event.AwaySportLeaguesId,
                    nova88Event.HomeSportTeamId,
                    nova88Event.AwaySportTeamId,
                    nova88Event.HomeOdds,
                    nova88Event.AwayOdds,
                    nova88Event.Handicap,
                    nova88Event.IsLiveStreaming,
                };
                
                if (yashBetHotEventDataDict.TryGetValue(key, out var yashBetEventData))
                {
                    if (nova88Event.LastUpdateTime > nowTime.AddHours(-3))
                    {
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
                        yashBetEventData.DataType = 1;
                    }
                    yashBetEventData.DataType = 2;
                }
                else
                {
                    yashBetEventData = nova88Event;
                    yashBetEventData.DataType = 0;
                }
                
                _logger.LogInformation($"SQ : {yashBetEventData.SQ} , DataType : {yashBetEventData.DataType}");
                //DataType, 0 = 新增 , 1 = 更新 , 2 = DB有了且沒變更 不做事
                switch (yashBetEventData.DataType)
                {
                    case 0:
                        result = await InsertHotEventInfoToYashBetDB(yashBetEventData);
                        break;
                    case 1:
                        result = await UpdateHotEventInfoToYashBetDB(yashBetEventData);
                        break;
                    case 2:
                        //不做事
                        break;
                }
            }
            
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
        
        private async Task<int> InsertHotEventInfoToYashBetDB(HotEventInfo hotEventData)
        {
            var result = await _yashBetDBDataLayer.InsertHotEventInfoToYashBetDB(hotEventData);
            return result;
        }

        private async Task<int> UpdateHotEventInfoToYashBetDB(HotEventInfo hotEventData)
        {
            var result = await _yashBetDBDataLayer.UpdateHotEventInfoToYashBetDB(hotEventData);
            return result;
        }
        
        #endregion 

    }
}