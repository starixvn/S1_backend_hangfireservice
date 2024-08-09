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
            var getSiteTicketKey = await GetSiteTicketKey(siteId);
            var getFPSResponse = await GetFPSData(getSiteTicketKey.TicketKey,siteName,token,apiUrl);

            #region Dev
            
            lastTrackingNumber = getFPSResponse.LastOrDefault().trackingNumber;
            var setTicketKey = await SetSiteTicketKey(siteId, lastTrackingNumber);

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
                    DeviceId = item.device ?? "NA",
                    DeviceType = item.deviceType,
                    Browser = item.browser ,
                    Domain = item.domain,
                    IP = item.ip,
                    RobotTracking = item.robotTracking,
                    //目前還沒有傳RiskLevel 需要再跟FPS追蹤
                    //RiskLevel = item.riskLevel,
                    RiskLevel = item.riskLevel ?? "NA",
                    UpdateTime = item.dateTime

                };
                var insertResult = _mainDBDataLayer.InsertCustFPS(insertCustFPSDto);
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

        private async Task<SiteTicketKey> GetSiteTicketKey(int siteId)
        {
            var result = await _mainDbClient.QueryAsync<SiteTicketKey>("Service_SiteTicketKey_Get", new
            {
                SiteId = siteId,
                TpId = "999",
                WorkerType = 1
            });
            
            return result.FirstOrDefault();
        }

        private async Task<int> SetSiteTicketKey(int siteId,long ticketKey)
        {
            var parameter = new DynamicParameters(new
            {
                SiteId = siteId,
                TpId = "999",
                WorkerType = 1,
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
            //Console.WriteLine(int.TryParse(custIdStr, out var custId) ? $"CustId: {custId}" : "無法轉換為數字");
            return custId;
        }
        
    }
}