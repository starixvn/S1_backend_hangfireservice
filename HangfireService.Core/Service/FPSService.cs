using System.Net;
using System.Text.RegularExpressions;
using Dapper;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Helpers;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Options;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HangfireService.Core.Service
{
    public class FPSService
    {
        private readonly HttpHelper _httpHelper;
        private readonly MySqlClient _mainDbClient;
        private readonly ILogger<FPSService> _logger;
        private readonly MainDBDataLayer _mainDBDataLayer;
        
        
        public FPSService(IServiceProvider provider)
        {
            _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _logger = provider.GetRequiredService<ILogger<FPSService>>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public async Task<int> GetFPSDataAsyncJob(int siteId,string siteName,string token,string apiUrl)
        {
            long lastTrackingNumber = 0;
            var getSiteTicketKey = await GetSiteTicketKey(siteId,1);
            var getFPSResponse = await GetFPSData(getSiteTicketKey.TicketKey,siteName,token,apiUrl);

            #region Dev
            
            lastTrackingNumber = getFPSResponse.LastOrDefault().trackingNumber;
            var setTicketKey = await SetSiteTicketKey(siteId, lastTrackingNumber,1);

            var filteredTransactions = getFPSResponse.Where(t => t.account.StartsWith("D"));
            getFPSResponse = filteredTransactions;
            
            #endregion

            if (!getFPSResponse.Any()) return setTicketKey;
            
            foreach (var item in getFPSResponse)
            {
                var insertCustFPSDto = new CustFPSDto()
                {
                    SiteId = siteId,
                    CustId = FilterCustId(item.account),
                    UserName = item.account,
                    TrackingNumber = item.trackingNumber,
                    Action = item.action,
                    DeviceId = string.IsNullOrWhiteSpace(item.device) ? "NA" : item.riskLevel,
                    DeviceType = item.deviceType,
                    Browser = item.browser ,
                    Domain = item.domain,
                    IP = item.ip,
                    RobotTracking = item.robotTracking,
                    RiskLevel = string.IsNullOrWhiteSpace(item.riskLevel) ? "NA" : item.riskLevel,
                    //抓回來是GMT-4 直接強加4小時變成+0
                    UpdateTime = item.dateTime.AddHours(4)

                };
                var insertResult = _mainDBDataLayer.InsertCustFPS(insertCustFPSDto);
            }

            // lastTrackingNumber = getFPSResponse.LastOrDefault().trackingNumber;
            // var setTicketKey = await SetSiteTicketKey(siteId, lastTrackingNumber);
            
            return setTicketKey;
        }

        public async Task<int> GetFPSRiskLevelDataAsyncJob(int siteId, string siteName, string token, string apiUrl)
        {
            long lastTrackingNumber = 0;
            var getSiteTicketKey = await GetSiteTicketKey(siteId,2);
            var getFPSRiskLevelResponse = await GetFPSRiskLevelData(getSiteTicketKey.TicketKey,siteName,token,apiUrl);
            
            #region Dev
            
            lastTrackingNumber = getFPSRiskLevelResponse.LastOrDefault().id;
            var setTicketKey = await SetSiteTicketKey(siteId, lastTrackingNumber,2);

            var filteredTransactions = getFPSRiskLevelResponse.Where(t => t.account.StartsWith("D"));
            getFPSRiskLevelResponse = filteredTransactions;
            
            #endregion
            
            if (!getFPSRiskLevelResponse.Any()) return setTicketKey;
            
            foreach (var item in getFPSRiskLevelResponse)
            {
                var insertCustFPSRiskLevelDto = new CustFPSRiskLevelDto()
                {
                    CustId = FilterCustId(item.account),
                    //目前還沒有傳RiskLevel 需要再跟FPS追蹤
                    RiskLevel = string.IsNullOrWhiteSpace(item.riskLevel) ? "NA" : item.riskLevel,
                };
                var insertResult = _mainDBDataLayer.InsertCustFPSRiskLevel(insertCustFPSRiskLevelDto);
            }

            // lastTrackingNumber = getFPSResponse.LastOrDefault().trackingNumber;
            // var setTicketKey = await SetSiteTicketKey(siteId, lastTrackingNumber);
            
            return setTicketKey;
            
        }

        private async Task <IEnumerable<FPSTransaction>> GetFPSData(long ticketKey,string siteName, string token, string apiUrl)
        {
            var url = $"{apiUrl}/api/GetTransactionBySubscriber/{siteName}/{ticketKey}";
            var headerParams = new Dictionary<string, string>
            {
                { "Authorization", token },
            };

            using var result = await _httpHelper.HttpGetAsync(url, headerParams, "application/json");
            if (result.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError($"GetFPSData httpResponse error URL : {url}  StatusCode: {result.StatusCode}");
                return null;
            }

            var contentResult = await result.Content.ReadAsStringAsync();
            var resultResp = JsonConvert.DeserializeObject<FPSResponse>(contentResult);

            return resultResp.trans;
        }
        
        private async Task <IEnumerable<FPSRiskLevel>> GetFPSRiskLevelData(long ticketKey,string siteName, string token, string apiUrl)
        {
            var url = $"{apiUrl}/api/GetRiskLevelBySubscriber/{siteName}/{ticketKey}";
            var headerParams = new Dictionary<string, string>
            {
                { "Authorization", token },
            };

            using var result = await _httpHelper.HttpGetAsync(url, headerParams, "application/json");
            if (result.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError($"GetFPSRiskLevelData httpResponse error URL : {url}  StatusCode: {result.StatusCode}");
                return null;
            }

            var contentResult = await result.Content.ReadAsStringAsync();
            var resultResp = JsonConvert.DeserializeObject<FPSRiskLevelResponse>(contentResult);

            return resultResp.trans;
        }

        private async Task<SiteTicketKey> GetSiteTicketKey(int siteId,int workerType)
        {
            var result = await _mainDbClient.QueryAsync<SiteTicketKey>("Service_SiteTicketKey_Get", new
            {
                SiteId = siteId,
                TpId = "999",
                WorkerType = workerType
            });
            
            return result.FirstOrDefault();
        }

        private async Task<int> SetSiteTicketKey(int siteId,long ticketKey,int workerType)
        {
            var parameter = new DynamicParameters(new
            {
                SiteId = siteId,
                TpId = "999",
                WorkerType = workerType,
                TicketKey = ticketKey
            });

            return await _mainDbClient.ExecuteAsync("Service_SiteTicketKey_Set", parameter);
        }

        private static int FilterCustId(string accountName)
        {
            var regex = new Regex(@"_(\d+)$");
            var match = regex.Match(accountName);
            var custIdStr = match.Groups[1].Value;
            int.TryParse(custIdStr, out var custId);
            return custId;
        }
        
    }
}