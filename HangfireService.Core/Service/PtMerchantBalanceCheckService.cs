using System.Net;
using HangfireService.Core.Helpers;
using HangfireService.Core.Jobs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HangfireService.Core.Service
{
    public class PtMerchantBalanceCheckService
    {
        
        private readonly HttpHelper _httpHelper;
        private readonly string _commonServiceHost;
        private readonly string _tpServiceHost;
        private readonly string _letsTalkGroupName;
        private readonly ILogger<BaseJob> _logger;

        private readonly Dictionary<string, PtCheckMerchantBalanceOption> _tpCheckMerchantConfig;
        private readonly Dictionary<int, string> _tpIdMapToSiteName;

        public PtMerchantBalanceCheckService(IServiceProvider provider)
        {
            var iConfiguration = provider.GetRequiredService<IConfiguration>();
            _tpCheckMerchantConfig = iConfiguration.GetSection("TpCheckMerchantBalanceConfig").Get<Dictionary<string, PtCheckMerchantBalanceOption>>();
            _commonServiceHost = iConfiguration.GetValue<string>("AiLabServiceHost:CommonServiceHost");
            _tpServiceHost = iConfiguration.GetValue<string>("AiLabServiceHost:TpServiceHost");
            _letsTalkGroupName = iConfiguration.GetValue<string>("LetsTalkConfig:SNS:LetsTalkGroupName");
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _logger = provider.GetRequiredService<ILogger<BaseJob>>();
            _tpIdMapToSiteName = new Dictionary<int, string>()
            {
                { (int)SiteId.Nova88, "NOVA88" },
                { (int)SiteId.Goal123, "GOAL123" },
                { (int)SiteId.Bwing, "BWING" },
            };
        }

        public async Task<object> RunJobAsync()
        {
            foreach (var config in _tpCheckMerchantConfig)
            {
                await CheckMerchantBalance(config.Value);
            }

            return true;
        }

        /// <summary>
        /// 確認商戶餘額
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private async Task CheckMerchantBalance(PtCheckMerchantBalanceOption config)
        {
            try
            {
                var postData = new
                {
                    TpId = config.TpId,
                    SiteId = config.SiteId,
                    TpCurrencyId = config.TpCurrencyId
                };

                var parameters = JsonConvert.SerializeObject(postData);
                var httpResponse = await _httpHelper.HttpPost1Async($"{_tpServiceHost}/api/GetMerchantBalance", null, parameters, "application/json");

                if (httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var merchantBalanceResponse = JsonConvert.DeserializeObject<MerchantBalanceResponse>(content);
                    var merchantBalance = merchantBalanceResponse.ResponseData.MerchantBalance;
                    if (merchantBalance < config.LessThanBalance) await SandLesTalkMessage(config, merchantBalanceResponse); //餘額低於最低限制，就發通知給客服
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,ex.Message);
            }
            finally
            {
                await Task.CompletedTask;
            }
        }

        /// <summary>
        /// 發 LesTalkMessage 到群組
        /// </summary>
        /// <param name="config"></param>
        /// <param name="merchantBalanceResponse"></param>
        /// <returns></returns>
        private async Task SandLesTalkMessage(PtCheckMerchantBalanceOption config, MerchantBalanceResponse merchantBalanceResponse)
        {
            try
            {
                var message = GetAlertMessage(config, merchantBalanceResponse);
                var postData = new
                {
                    GroupName = _letsTalkGroupName,
                    Message = message
                };
                var parameters = JsonConvert.SerializeObject(postData);

                var httpResponse = await _httpHelper.HttpPost1Async($"{_commonServiceHost}/Common/LetsTalk/SendMessage",
                    null, parameters, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            finally
            {
                await Task.CompletedTask;
            }
        }

        /// <summary>
        /// 取得發送訊息
        /// </summary>
        /// <param name="config"></param>
        /// <param name="merchantBalanceResponse"></param>
        /// <returns></returns>
        private string GetAlertMessage(PtCheckMerchantBalanceOption config, MerchantBalanceResponse merchantBalanceResponse)
        {
            var message =
                $"* PlayTech Balance Alert * \r\n" +
                $"* Time : {DateTime.UtcNow.AddHours(8):yyyy-MM-dd HH:mm:ss} * \r\n" +
                $"=======================\r\n" +
                $"The balance of playTech is less than {config.LessThanBalance}.\r\n" +
                $"Please add balance.\r\n" +
                $"=======================\r\n" +
                $"Site：{_tpIdMapToSiteName[config.SiteId]}\r\n" +
                $"TPName : PlayTech \r\n" +
                $"MerchantName : {merchantBalanceResponse.ResponseData.MerchantName}\r\n" +
                $"MerchantBalance : {merchantBalanceResponse.ResponseData.MerchantBalance}\r\n" +
                $"Currency:{merchantBalanceResponse.ResponseData.CurrencyName}";
            return message;
        }

        #region Response Model

        public class ResponseData
        {
            [JsonProperty("merchantBalance")]
            public decimal MerchantBalance { get; set; }

            [JsonProperty("merchantName")]
            public string MerchantName { get; set; }

            [JsonProperty("currencyName")]
            public string CurrencyName { get; set; }
        }

        public class MerchantBalanceResponse
        {
            [JsonProperty("data")]
            public ResponseData ResponseData { get; set; }

            [JsonProperty("errorCode")]
            public int ErrorCode { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("rawErrorCode")]
            public string RawErrorCode { get; set; }

            [JsonProperty("rawMessage")]
            public string RawMessage { get; set; }
        }

        #endregion
    }
}
