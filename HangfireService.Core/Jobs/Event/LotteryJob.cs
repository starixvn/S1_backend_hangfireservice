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
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Serilog.Context;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("LotteryJob", queue: "Ticket")]
    public class LotteryJob : BaseJob, IScheduledJob
    {
        private readonly ReportDBDataLayer _reportDbDataLayer;
        private readonly MainDBDataLayer _mainDbDataLayer;
        public LotteryJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _reportDbDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
            _mainDbDataLayer = provider.GetRequiredService<MainDBDataLayer>();
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");
                var option = JsonConvert.DeserializeObject<LotteryOption>(jobOption.Parameters);
                
                var lotteryEventDrawSetting = _reportDbDataLayer.GetLotteryEventDrawSetting(option.SiteId, DateTime.UtcNow.Date);
                
                var now = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8));
                if (lotteryEventDrawSetting is not null 
                    && lotteryEventDrawSetting.Status == (int)LotteryEventDrawEnum.Running
                    && now >= lotteryEventDrawSetting.StartDate
                    && now <= lotteryEventDrawSetting.EndDate)
                {
                    _logger.LogInformation($"{jobName} running ||  Site :{option.SiteId} || RoundId :{lotteryEventDrawSetting.RoundId} || Range :{lotteryEventDrawSetting.StartDate} ~ {lotteryEventDrawSetting.EndDate}" );
                    
                    var ticketStickerSetting = _reportDbDataLayer.GetRaffleTicketStickerSetting(option.SiteId).ToList();
                 
                    var currencyList = ticketStickerSetting.Select(x => x.CurrencyId);
                
                    var summaryList = _reportDbDataLayer.GetLotteryTurnOverSummary(
                        option.SiteId, 
                        JsonConvert.SerializeObject(currencyList),
                        lotteryEventDrawSetting.StartDate.UtcDateTime,
                        lotteryEventDrawSetting.EndDate.UtcDateTime);

                    foreach (var cust in summaryList)
                    {
                        var setting = ticketStickerSetting.FirstOrDefault(x => x.CurrencyId == cust.CurrencyId);
                        var turnOver = setting.TurnOver;
                        
                        var ticketCount = (int) ( cust.ValidBet / turnOver );
                        var beforeCustTicket = _reportDbDataLayer.GetCustLotteryTicket(cust.CustId, lotteryEventDrawSetting.RoundId , (int)LotteryTicketEnum.Ticket)
                            .Where(x=>x.Type == 1).ToList();

                        var dealCount = ticketCount - beforeCustTicket.Count;

                        if (dealCount <= 0)
                        {
                            continue;
                        }
                        
                        //發放
                        await using var conn = _mainDbDataLayer.GetMySqlConnection();
                        conn.Open();
                        var trans = await conn.BeginTransactionAsync();
                        
                        try
                        {
                            var insertTicketList = CreateCustLotteryTicket(dealCount, cust.CustId, lotteryEventDrawSetting.RoundId);
                            var insertResult = _mainDbDataLayer.InsertCustLotteryTicket(insertTicketList ,trans);

                            if (insertResult > 0)
                            { 
                                trans.Commit();
                            }
                            else
                            {
                                await trans.DisposeAsync();
                            }
                        }
                        catch (MySqlException ex)
                        {
                            if (ex.Number == (int)MySqlErrorCode.DuplicateKeyEntry) //匯入資料重複
                            {
                                var insertTicketListReTry = CreateCustLotteryTicket(dealCount, cust.CustId, lotteryEventDrawSetting.RoundId);
                                var insertResult = _mainDbDataLayer.InsertCustLotteryTicket(insertTicketListReTry ,trans);

                                if (insertResult > 0)
                                { 
                                    trans.Commit();
                                }
                                else
                                {
                                    await trans.DisposeAsync();
                                }
                            }
                        }
                        catch(Exception ex)
                        {
                            _logger.LogInformation($"{jobName} running || Ticket Insert Failed ||  CustId :{cust.CustId} || EX: {ex.Message}" );
                        }
                        finally
                        {
                            await conn.CloseAsync();
                            await conn.DisposeAsync();
                        }
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
        
        private List<CustLotteryTicket> CreateCustLotteryTicket(int count , int custId, int roundId)
        {
            var insertTicketList = new List<CustLotteryTicket>();
            for (var i = 1; i <= count; i++)
            {
                var ticket = new CustLotteryTicket
                {
                    CustId = custId,
                    RoundId = roundId,
                    StickerId = 0,
                    TicketNo = RandomTicketNo(),
                    Type = (int)LotteryTicketEnum.Ticket
                };
                insertTicketList.Add(ticket);
            }

            return insertTicketList;
        }
        
        private string RandomTicketNo()
        {
            var random = new Random();
            var randomTicketNo = string.Empty;

            for (var i = 0; i < 10; i++)
            {
                randomTicketNo += random.Next(10).ToString();
            }

            return randomTicketNo;
        }
    }
}
