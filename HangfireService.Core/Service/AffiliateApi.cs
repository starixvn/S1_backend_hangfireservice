using System.Net;

using HangfireService.Core.Helpers;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Options;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

namespace HangfireService.Core.Service
{
    public class AffiliateApi
    {
        private readonly HttpHelper _httpHelper;
        private readonly MpaExchangeRateConfig _mpaExchangeRateConfig;
        private readonly ILogger<AffiliateApi> _logger;

        public AffiliateApi(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _mpaExchangeRateConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<MpaExchangeRateConfig>("MpaExchangeRate");
            _logger = provider.GetRequiredService<ILogger<AffiliateApi>>();
        }

        /// <summary>
        /// 登入驗證，取得Token
        /// </summary>
        /// <returns>AccessToken</returns>
        public async Task<string> AuthGetAccessToken()
        {
            try
            {
                var postData = new
                {
                    _mpaExchangeRateConfig.Username,
                    _mpaExchangeRateConfig.Password,
                };

                var parameters = JsonConvert.SerializeObject(postData);
                var httpResponse = await _httpHelper.HttpPost1Async($"{_mpaExchangeRateConfig.ApiUrl}/Auth", null, parameters, "application/json");

                if (httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var response = JsonConvert.DeserializeObject<AuthResponse>(content);
                    return response.AccessToken;
                }

                var failResponseBody = await httpResponse.Content.ReadAsStringAsync();
                _logger.LogError($"{_mpaExchangeRateConfig}/Auth api fail statusCode:{httpResponse.StatusCode} responseBody :{failResponseBody}");
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $@"AffiliateApi.AuthGetAccessToken:{ex.Message}");
            }
            finally
            {
                await Task.CompletedTask;
            }

            return string.Empty;
        }
        public async Task<List<Affiliate>> QueryAffDetailInfoList(int pageIndex, int pageSize, string siteName, string accessToken)
        {
            var postData = new
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                LicenseeCode = siteName
            };
            var parameters = JsonConvert.SerializeObject(postData);
            var httpResponse = await _httpHelper.HttpPost1Async($"{_mpaExchangeRateConfig.ApiUrl}/AffDetailInfoList?access_token={accessToken}", null, parameters, "application/json");
            var content = await httpResponse.Content.ReadAsStringAsync();
            var affiliates = JsonConvert.DeserializeObject<List<Affiliate>>(content);
            return affiliates;
        }

        private class AuthResponse
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }

            [JsonProperty("token_type")]
            public string TokenType { get; set; }

            [JsonProperty("expires_in")]
            public int Expires { get; set; }
        }
        public class Affiliate
        {
            public string AffiliateCode { get; set; }
            public string ParentAffiliateName { get; set; }
            public string AffiliateName { get; set; }
            public string PlayerName { get; set; }
            public string Currency { get; set; }
            public DateTime RegisteredDate { get; set; }
            public DateTime? LastLoginDate { get; set; }
            public string LastLoginIP { get; set; }
        }
    }
}
