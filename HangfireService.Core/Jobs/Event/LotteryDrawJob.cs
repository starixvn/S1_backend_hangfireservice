using System.ComponentModel;
using Hangfire;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Server;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Models.Options;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("LotteryDrawJob", queue: "Ticket")]
    public class LotteryDrawJob : BaseJob, IScheduledJob
    {
        private readonly ReportDBDataLayer _reportDbDataLayer;
        private readonly MainDBDataLayer _mainDbDataLayer;
        private readonly Dictionary<LotteryPrizeCategory, LotteryDrawCount> _prizeList;
        
        public LotteryDrawJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _reportDbDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
            _mainDbDataLayer = provider.GetRequiredService<MainDBDataLayer>();
            _prizeList = new Dictionary<LotteryPrizeCategory, LotteryDrawCount>
            {
                {LotteryPrizeCategory.FirstPrize, new LotteryDrawCount {DrawCount = 1 , Name = "First" }},
                {LotteryPrizeCategory.SecondPrize, new LotteryDrawCount {DrawCount = 1 ,Name = "Second"}},
                {LotteryPrizeCategory.ThirdPrize,new LotteryDrawCount {DrawCount = 1 ,Name = "Third"}},
                {LotteryPrizeCategory.ConsolationPrize, new LotteryDrawCount {DrawCount = 20 ,Name = "Consolation"}},
                {LotteryPrizeCategory.SmallPrize, new LotteryDrawCount {DrawCount = 10 , Name = "Small"}},
            };
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");
                var option = JsonConvert.DeserializeObject<LotteryOption>(jobOption.Parameters);
                
                var lotteryEventDrawSetting = _reportDbDataLayer.GetLotteryEventDrawSetting(option.SiteId, DateTime.UtcNow.Date);
                
                if (lotteryEventDrawSetting is not null && lotteryEventDrawSetting.Status == (int)LotteryEventDrawEnum.Running)
                {
                    _logger.LogInformation($"{jobName} running ||  Site :{option.SiteId} || RoundId :{lotteryEventDrawSetting.RoundId} || Range :{lotteryEventDrawSetting.StartDate} ~ {lotteryEventDrawSetting.EndDate}" );
                    
                    #region DealSettingData

                    var excludeList = JsonConvert.DeserializeObject<List<int>>(lotteryEventDrawSetting.ExcludeSetting);
                    
                    // 設定每個獎項的 ExcludeCount
                    LotteryPrizeCategory[] categories = 
                    {
                        LotteryPrizeCategory.FirstPrize, 
                        LotteryPrizeCategory.SecondPrize, 
                        LotteryPrizeCategory.ThirdPrize, 
                        LotteryPrizeCategory.ConsolationPrize, 
                        LotteryPrizeCategory.SmallPrize
                    };

                    for (var i = 0; i < categories.Length; i++)
                    {
                        if (_prizeList.ContainsKey(categories[i]))
                        {
                            _prizeList[categories[i]].ExcludeCount = excludeList[i];
                            _prizeList[categories[i]].DrawCount -= excludeList[i];
                        }
                    }
                    
                    #endregion
                    
                    var originTicketList = _reportDbDataLayer.GetCustLotteryTicket(0, lotteryEventDrawSetting.RoundId , (int)LotteryTicketEnum.Ticket).ToList();
                    
                   
                    var ticketList = originTicketList.ToList();

                    #region 排除用戶

                    var custEventRestriction = _reportDbDataLayer.GetEventRestriction(option.SiteId);
                    ticketList.RemoveAll(x => custEventRestriction.Select(y=>y.CustId).Contains(x.CustId));
                    
                    #endregion
                    
                    var random = new Random();
                    var insertData = new LotteryTicketHistory
                    {
                        RoundId = lotteryEventDrawSetting.RoundId,
                        StickerId = random.Next(1, 13)
                    };
                    
                    foreach (var prize in _prizeList)
                    {
                        var winnerList = new List<string>();
                        
                        // 實作排除設定的假資料
                        for (var i = 1; i <= prize.Value.ExcludeCount; i++)
                        {
                            var fakeTicketNo = RandomTicketNo(originTicketList);
                            winnerList.Add(fakeTicketNo);
                        }
                        
                        //實際抽獎
                        for (var i = 1; i <= prize.Value.DrawCount; i++)
                        {
                            if (ticketList.Count > 0)
                            {
                                //隨機抽一個
                                var index = random.Next(ticketList.Count);
                                
                                //中獎者
                                var selectedElement = ticketList[index];
                                
                                winnerList.Add(selectedElement.TicketNo);
                                
                                //排除抽獎箱的票
                                ticketList.RemoveAt(index);
                                ticketList.RemoveAll(x => x.CustId == selectedElement.CustId);
                            }
                            //實際的票抽完了, 塞入假資料
                            else
                            {
                                var fakeTicketNo = RandomTicketNo(originTicketList);
                                winnerList.Add(fakeTicketNo);
                            }
                        }

                        switch (prize.Key)
                        {
                            case LotteryPrizeCategory.FirstPrize:
                                insertData.FirstPrize = JsonConvert.SerializeObject(winnerList);
                                break;
                            case LotteryPrizeCategory.SecondPrize:
                                insertData.SecondPrize = JsonConvert.SerializeObject(winnerList);
                                break;
                            case LotteryPrizeCategory.ThirdPrize:
                                insertData.ThirdPrize = JsonConvert.SerializeObject(winnerList);
                                break;
                            case LotteryPrizeCategory.ConsolationPrize:
                                insertData.ConsolationPrize = JsonConvert.SerializeObject(winnerList);
                                break;
                            case LotteryPrizeCategory.SmallPrize:
                                insertData.SmallPrize = JsonConvert.SerializeObject(winnerList);
                                break;
                        }
                    }
                    
                    await using var conn = _mainDbDataLayer.GetMySqlConnection();
                    conn.Open();
                    var trans = await conn.BeginTransactionAsync();
                        
                    try
                    {
                        var insertResult = _mainDbDataLayer.InsertLotteryTicketHistory(insertData , trans) > 0;
                    
                        if (insertResult)
                        {
                            var setResult = _mainDbDataLayer.LotteryEventDrawSettingSet(option.SiteId, lotteryEventDrawSetting.RoundId, (int) LotteryEventDrawEnum.Done ,trans) > 0 ;
                            if (setResult) 
                            {
                                trans.Commit();
                            }
                            else
                            {
                                await trans.DisposeAsync();
                            }
                        }
                        else
                        {
                            await trans.DisposeAsync();
                        }
                    }
                    catch(Exception ex)
                    {
                        _logger.LogInformation($"{jobName} running || TicketHistory Insert Failed || EX: {ex.Message}" );
                    }
                    finally
                    {
                        await conn.CloseAsync();
                        await conn.DisposeAsync();
                    }
                    
                    // var levelTurnOverSetting = _reportDbDataLayer.GetLotteryEventDrawTurnOverSetting(option.SiteId).ToList();
                    // foreach (var setting in levelTurnOverSetting.Where(setting => setting.TurnOverEnd == 0))
                    // {
                    //     setting.TurnOverEnd = decimal.MaxValue;
                    // }
                    // var drawPrizeList = _reportDbDataLayer.GetDrawPrizeList(21, 3);
                    // var custTurnOver = _reportDbDataLayer.GetCustLotteryTurnOverSummary(
                    //     selectedElement.CustId, lotteryEventDrawSetting.StartDate,
                    //     lotteryEventDrawSetting.EndDate);
                    // var levelData = levelTurnOverSetting.FirstOrDefault(x => x.CurrencyId == selectedElement.CurrencyId && x.TurnOverStart <= custTurnOver && x.TurnOverEnd <= custTurnOver);
                    // var prizeData = drawPrizeList.FirstOrDefault(x => x.Tier == levelData.Level && x.PrizeName.Contains(prize.Value.Name));
                    
                }
                
                return JsonConvert.SerializeObject(new
                {
                    Success = "true"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return JsonConvert.SerializeObject(ex);
            }
        }
        
        private string RandomTicketNo(List<CustLotteryTicket> tickets)
        {
            var random = new Random();
            var randomTicketNo = string.Empty;

            do
            {
                for (var i = 0; i < 10; i++)
                {
                    randomTicketNo += random.Next(10).ToString();
                }
            } while ( tickets.Select(x=>x.TicketNo).Contains(randomTicketNo) );
            
            return randomTicketNo;
        }
        
    }
}
