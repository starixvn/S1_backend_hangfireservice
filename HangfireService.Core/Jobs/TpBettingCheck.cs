using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Net;
using Hangfire.Server;
using Hangfire;
using HangfireService.Core.Helpers;
using HangfireService.Core.Models.DBs;
using Microsoft.Extensions.Configuration;

namespace HangfireService.Core.Jobs
{
    public class TpBettingCheck : BaseJob, IScheduledJob
    {
        private readonly MainDBDataLayer _mainDbDataLayer;
        private readonly ReportDBDataLayer _reportDbDataLayer;
        private readonly HttpHelper _httpHelper;
        private readonly string _letsTalkGroupName;
        private readonly string _commonServiceHost;
        
        public TpBettingCheck(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _mainDbDataLayer = provider.GetRequiredService<MainDBDataLayer>();
            _reportDbDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            var iConfiguration = provider.GetRequiredService<IConfiguration>();
            _letsTalkGroupName = iConfiguration.GetValue<string>("LetsTalkConfig:LetsTalkGroupName");
            _commonServiceHost = iConfiguration.GetValue<string>("AiLabServiceHost:CommonServiceHost");
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption , PerformContext context, IJobCancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");

                var monitorDatas = _reportDbDataLayer.GetTpBettingCheck();
                var lastBetting = _reportDbDataLayer.GetLastBettingTime(DateTime.UtcNow.AddMinutes(-100000) , DateTime.UtcNow);

                foreach (var data in monitorDatas)
                {
                    var lastBetData = lastBetting.FirstOrDefault(x => x.SiteId == data.SiteId
                                                                      && x.CurrencyId == data.CurrencyId
                                                                      && x.TpId == data.TpId
                                                                      && x.GameType == data.GameType);
                    if (lastBetData != null)
                    {
                        data.LastBettingTime = lastBetData.LastBettingTime;
                        _mainDbDataLayer.SetLastBettingTime(data.SiteId,data.CurrencyId,data.TpId,data.GameType,lastBetData.LastBettingTime,false);
                    }

                    var checkTime = DateTime.UtcNow.AddMinutes(data.Frequency * -1);
                    if (checkTime > data.LastExecuteTime && checkTime > data.LastBettingTime)
                    {
                        _mainDbDataLayer.SetLastBettingTime(data.SiteId,data.CurrencyId,data.TpId,data.GameType,null,true);
                        
                        var maintain = _mainDbDataLayer.GetTpMaintainList(data.SiteId)
                            .Where(item => item.MaintainCheck(DateTime.UtcNow))
                            .Where(x => x.SiteId == data.SiteId
                                        && x.TpId == data.TpId
                                        && x.TpGameType == data.GameType);
                        
                        if (!maintain.Any())
                        {
                            await SandLesTalkMessage(data, data.LastBettingTime);
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {            
                _logger.LogError(ex, ex.Message);
                return JsonConvert.SerializeObject(ex);
            }
        }
        
        private async Task<bool> SandLesTalkMessage(TpBettingCheckModel smsOption, DateTime lastBettingTime)
        {
            try
            {
                var postData = new
                {
                    GroupName = _letsTalkGroupName,
                    Message = MakeAlertMessage(smsOption, lastBettingTime)
                };

                var parameters = JsonConvert.SerializeObject(postData);
                var httpResponse = await _httpHelper.HttpPost1Async($"{_commonServiceHost}/Common/LetsTalk/SendMessage",
                    null, parameters, "application/json");
                if (httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return false;
            }
            return false;
        }

        private static string MakeAlertMessage(TpBettingCheckModel model, DateTime lastBettingTime)
        {
            var message =
                    $"* Betting Monitor Alert * \r\n" +
                    $"* Time : {DateTime.UtcNow.AddHours(8):yyyy-MM-dd HH:mm:ss} * \r\n" +
                    $"=======================\r\n" +
                    $"Product : {model.TypeName}\r\n" +
                    $"Currency : {model.Currency}\r\n" +
                    $"Duration : {model.Frequency} Minutes\r\n" +
                    $"Last Ticket Time : {lastBettingTime.AddHours(8):yyyy-MM-dd HH:mm:ss} (GMT+8) \r\n" 
                ;
            return message;
        }
    }
}