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
using HangfireService.Core.Service;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Serilog.Context;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("LotteryPayoutJob", queue: "Ticket")]
    public class LotteryPayoutJob : BaseJob, IScheduledJob
    {
        private readonly ReportDBDataLayer _reportDbDataLayer;
        private readonly MainDBDataLayer _mainDbDataLayer;
        private readonly FincanceService _financeService;
            
        public LotteryPayoutJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _reportDbDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
            _mainDbDataLayer = provider.GetRequiredService<MainDBDataLayer>();
            _financeService = provider.GetRequiredService<FincanceService>();
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");
                var option = JsonConvert.DeserializeObject<LotteryOption>(jobOption.Parameters);
                var siteId = option.SiteId;
                
                var lotteryEventDrawSetting = _reportDbDataLayer.GetLotteryEventDrawSetting(siteId, null);
                var now = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8));
                var currentEventDraw = lotteryEventDrawSetting.FirstOrDefault(x => 
                    x.Status == (int) LotteryEventDrawEnum.Done
                    //抽獎日隔天0點 才發錢
                    && x.DrawDate.Date.AddDays(1) <= now);
                
                if (currentEventDraw is not null)
                {
                    _logger.LogInformation($"{jobName} running ||  Site :{siteId} || RoundId :{currentEventDraw.RoundId}" );
                    
                    var custLotteryTicket = _reportDbDataLayer.GetCustLotteryTicket(0, currentEventDraw.RoundId , (int)LotteryTicketEnum.Ticket).ToList();
                    var levelTurnOverSetting = _reportDbDataLayer.GetLotteryEventDrawTurnOverSetting(siteId).ToList();
                    var drawPrizeList = await _reportDbDataLayer.GetPrizes(21, 3);
                    var jackpotData = drawPrizeList.FirstOrDefault(x => x.Jackpot == 1);
                    var jackpotAccumulation = _reportDbDataLayer.GetDrawPrizeJackpotAccumulation(jackpotData.EventId);
                    var rateDate = new DateTimeOffset(new DateTime(currentEventDraw.StartDate.Year, currentEventDraw.StartDate.Month, 1), TimeSpan.Zero);
                    var rateList = _reportDbDataLayer.GetMPARate(rateDate, rateDate, 2).ToList();
                    
                    #region DealData
                    
                    var winnerList = _reportDbDataLayer.GetLotteryTicketHistory(siteId, currentEventDraw.RoundId);
                    var winnerPayoutList = new List<WinnerPayoutData>();
                    var firstPrizeList = JsonConvert.DeserializeObject<List<string>>(winnerList.FirstPrize);
                    var secondPrizeList = JsonConvert.DeserializeObject<List<string>>(winnerList.SecondPrize);
                    var thirdPrizeList = JsonConvert.DeserializeObject<List<string>>(winnerList.ThirdPrize);
                    var consolationPrizeList = JsonConvert.DeserializeObject<List<string>>(winnerList.ConsolationPrize);
                    var smallPrizeList = JsonConvert.DeserializeObject<List<string>>(winnerList.SmallPrize);
                    winnerPayoutList.AddRange(firstPrizeList.Select(ticketNo => new WinnerPayoutData { TicketNo = ticketNo, LotteryPrizeCategory = LotteryPrizeCategory.FirstPrize , Name = "First"}));
                    winnerPayoutList.AddRange(secondPrizeList.Select(ticketNo => new WinnerPayoutData { TicketNo = ticketNo, LotteryPrizeCategory = LotteryPrizeCategory.SecondPrize  ,Name = "Second"}));
                    winnerPayoutList.AddRange(thirdPrizeList.Select(ticketNo => new WinnerPayoutData { TicketNo = ticketNo, LotteryPrizeCategory = LotteryPrizeCategory.ThirdPrize ,Name = "Third"}));
                    winnerPayoutList.AddRange(consolationPrizeList.Select(ticketNo => new WinnerPayoutData { TicketNo = ticketNo, LotteryPrizeCategory = LotteryPrizeCategory.ConsolationPrize ,Name = "Consolation"}));
                    winnerPayoutList.AddRange(smallPrizeList.Select(ticketNo => new WinnerPayoutData { TicketNo = ticketNo, LotteryPrizeCategory = LotteryPrizeCategory.SmallPrize ,Name = "Small"}));

                    foreach (var setting in levelTurnOverSetting.Where(setting => setting.TurnOverEnd == 0))
                    {
                        setting.TurnOverEnd = decimal.MaxValue;
                    }
                    
                    #endregion
                    
                    await using var conn = _mainDbDataLayer.GetMySqlConnection();
                    conn.Open();
                    var trans = await conn.BeginTransactionAsync();
                    
                    try
                    {
                        var isPayoutJackpot = false;
                        
                        foreach (var data in winnerPayoutList)
                        {
                            var ticketInfo = custLotteryTicket.FirstOrDefault(x => x.TicketNo == data.TicketNo);
                            //真實有中獎CustId , 非造假票
                            if (ticketInfo is not null)
                            {
                                var custTurnOver = _reportDbDataLayer.GetCustLotteryTurnOverSummary(ticketInfo.CustId, currentEventDraw.StartDate.UtcDateTime, currentEventDraw.EndDate.UtcDateTime);
                                var levelData = levelTurnOverSetting.FirstOrDefault(x => x.CurrencyId == ticketInfo.CurrencyId && x.TurnOverStart <= custTurnOver && x.TurnOverEnd >= custTurnOver);
                                if (levelData is null)
                                {
                                    throw new InvalidOperationException($"{jobName} running || {data.TicketNo} Ticket || Can't find levelData || ticketInfo :{ticketInfo} , custTurnOver : {custTurnOver} " );
                                }
                                
                                var prizeData = drawPrizeList.FirstOrDefault(x => x.Tier == levelData.Level && x.PrizeName.Contains(data.Name));
                                if (prizeData is not null)
                                {
                                    
                                    if (data.LotteryPrizeCategory == LotteryPrizeCategory.FirstPrize)
                                    {
                                        var custSticker = _reportDbDataLayer.GetCustLotteryTicket(ticketInfo.CustId,  currentEventDraw.RoundId, (int)LotteryTicketEnum.Sticker).FirstOrDefault();
                                        if (custSticker is not null && custSticker.StickerId == winnerList.StickerId)
                                        {
                                            isPayoutJackpot = true;
                                        }
                                    }
                                    
                                    var result = PayOutPrize(jobName, currentEventDraw.RoundId, siteId,winnerList.StickerId, rateList, ticketInfo, data, prizeData, isPayoutJackpot ,jackpotAccumulation, trans).Result;
                                }
                            }
                        }

                        //未發出去就累積, 發出去就重置 
                        var accumulationAmount = isPayoutJackpot ? jackpotData.Amount : jackpotAccumulation.JackpotAmount + jackpotData.Amount;
                        var jackpotAccumulationSetResult = _mainDbDataLayer.DrawPrizeJackpotAccumulationSet(jackpotData.PrizeId, accumulationAmount ,trans) > 0;
                        
                        // SET Finish
                        var setSettingResult = _mainDbDataLayer.LotteryEventDrawSettingSet(option.SiteId, currentEventDraw.RoundId, (int) LotteryEventDrawEnum.PayOut ,trans )  > 0;
                        
                        if (setSettingResult && jackpotAccumulationSetResult)
                        {
                            trans.Commit();
                        }
                        else
                        {
                            await trans.DisposeAsync();
                        }
                    }
                    catch(Exception ex)
                    {
                        throw new InvalidOperationException($"{jobName} running || Payout Job Failed || EX: {ex.Message}" );
                    }
                    finally
                    {
                        await conn.CloseAsync();
                        await conn.DisposeAsync();
                    }
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

        private async Task<bool> PayOutPrize( string jobName ,int roundId ,int siteId , int winStickerId , List<MpaRateDto> rateList ,CustLotteryTicket ticketInfo ,
            WinnerPayoutData data , Prize prizeData , bool isJackpotPayout, DrawPrizeJackpotAccumulation jackpotAccumulation , MySqlTransaction trans)
        {
            var rateDto = rateList.FirstOrDefault(x => x.CurrencyId == 3 && x.TargetCurrencyId == ticketInfo.CurrencyId);
            if (rateDto is null)
            {
                throw new InvalidOperationException($"{jobName} running || {data.TicketNo} Ticket || Can't find rate" );
            }
            
            var prizeAmount = prizeData.Amount * rateDto.Rate;
            var result = _mainDbDataLayer.LotteryDrawPrizeSet(data.TicketNo, prizeAmount, roundId, 
                (int)data.LotteryPrizeCategory, prizeData.EventId, prizeData.PrizeId, ticketInfo.CustId, DateTime.UtcNow, 0, 0, trans) > 0;

            #region Jackpot

            if (isJackpotPayout && result)
            {
                var jackpotAmount = jackpotAccumulation.JackpotAmount * rateDto.Rate;
                result = _mainDbDataLayer.LotteryDrawPrizeSet(null , jackpotAmount, roundId, 
                    (int)LotteryPrizeCategory.Jackpot, jackpotAccumulation.EventId, jackpotAccumulation.PrizeId, ticketInfo.CustId, DateTime.UtcNow, 1 , winStickerId , trans) > 0;
                
                if (result)
                {
                    var memo = $"PrizeName : {jackpotAccumulation.PrizeName} ,Prize: {jackpotAmount:#,##0.##}";
                    result = _financeService.SetBalance(ticketInfo.CustId, siteId, ticketInfo.UserName, jackpotAmount, 8, memo, jackpotAccumulation.EventId.ToString(), out _, out _, trans) > 0;
                    if (result == false)
                    {
                        await trans.DisposeAsync();
                        throw new InvalidOperationException($"{jobName} running || {data.TicketNo} Ticket || SetBalance Failed" );
                    }
                }
                else
                {
                    await trans.DisposeAsync();
                    throw new InvalidOperationException($"{jobName} running || {data.TicketNo} Ticket || DrawPrizeSet Failed" );
                }
            }

            #endregion
            
            if (result)
            {
                var memo = $"PrizeName : {prizeData.PrizeName} ,Prize: {prizeAmount:#,##0.##}";
                result = _financeService.SetBalance(ticketInfo.CustId, siteId, ticketInfo.UserName, prizeAmount, 8, memo, prizeData.EventId.ToString(), out _, out _, trans) > 0;
                if (result == false)
                {
                    await trans.DisposeAsync();
                    throw new InvalidOperationException($"{jobName} running || {data.TicketNo} Ticket || SetBalance Failed" );
                }
            }
            else
            {
                await trans.DisposeAsync();
                throw new InvalidOperationException($"{jobName} running || {data.TicketNo} Ticket || DrawPrizeSet Failed" );
            }

            return true ; 
        }
        
    }
}