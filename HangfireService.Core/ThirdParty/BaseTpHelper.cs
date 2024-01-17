using System.Text;

using HangfireService.Core.DataLayers;
using HangfireService.Core.Helpers;
using HangfireService.Core.Models.Finance;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

namespace HangfireService.Core.ThirdParty
{
    public class BaseTpHelper
    {
        public ILogger<BaseTpHelper> _logger { get; set; }

        protected readonly HttpHelper _httpHelper;

        private readonly WagerDBDataLayer _wagerDBDataLayer;

        private readonly MainDBDataLayer _mainDBDataLayer;

        public BaseTpHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<BaseTpHelper>>();
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _wagerDBDataLayer = provider.GetRequiredService<WagerDBDataLayer>();
            _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
        }

        public async Task<HttpResponseMessage> HttpPostAsync(string url, byte[] data, string name, string contentType)
        {
            try
            {
                _logger.LogWarning($"Tp.Request.URL:{url}");
                var formData = new MultipartFormDataContent();
                var streamContent = new StreamContent(new MemoryStream(data));
                streamContent.Headers.Add("Content-Type", contentType);
                streamContent.Headers.Add("Content-Disposition", $"form-data; name=\"{name}\"; filename=\"{name}\"");
                formData.Add(streamContent, "file", $"{name}");

                var response = await _httpHelper.GetClient().PostAsync(url, formData);

                _logger.LogWarning($"Tp.Request.Response:{JsonConvert.SerializeObject(response)}");

                response.EnsureSuccessStatusCode();
                return response;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
        protected async Task<HttpResponseMessage> HttpPostAsync(string url, Dictionary<string, string> parameters)
        {
            try
            {
                var response = await _httpHelper.GetClient().PostAsync(url, new FormUrlEncodedContent(parameters));
                response.EnsureSuccessStatusCode();
                return response;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        protected async Task<HttpResponseMessage> HttpPostAsync(string url, string parameters, string mediaType)
        {
            try
            {
                var response = await _httpHelper.GetClient().PostAsync(url, new StringContent(parameters, Encoding.UTF8, mediaType));
                response.EnsureSuccessStatusCode();
                return response;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        protected T FromJson<T>(string jsonString)
        {
            if (!string.IsNullOrEmpty(jsonString))
            {
                return JsonConvert.DeserializeObject<T>(jsonString);
            }
            else
            {
                return default;
            }
        }

        protected string ToJson(object t)
        {
            return JsonConvert.SerializeObject(t, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }

        public List<TpBetRecordCheck> ProcessTpBetRecordSubCurrencyToMainCurrency(List<TpBetRecordCheck> tpBetRecordData)
        {
            var mainCurrencyId = 3;
            var subCurrencyId = 72;
            bool hasMainCurrency = tpBetRecordData.Any(c => c.CurrencyId == mainCurrencyId);
            bool hasSubCurrency = tpBetRecordData.Any(c => c.CurrencyId == subCurrencyId);

            if (!hasMainCurrency && hasSubCurrency)
            {
                // 無主幣別資料將子幣別取代為主幣別
                ReplaceSubCurrencyWithMainCurrency(tpBetRecordData, mainCurrencyId, subCurrencyId);
            }
            else if (hasMainCurrency && hasSubCurrency)
            {
                // 加總
                AggregateCurrencyData(tpBetRecordData, mainCurrencyId, subCurrencyId);
            }
            return tpBetRecordData;
        }

        private void ReplaceSubCurrencyWithMainCurrency(List<TpBetRecordCheck> tpBetRecordData, int mainCurrencyId, int subCurrencyId)
        {
            TpBetRecordCheck subCurrencyRecord = tpBetRecordData.FirstOrDefault(i => i.CurrencyId == subCurrencyId);
            subCurrencyRecord.CurrencyId = mainCurrencyId;
        }

        private void AggregateCurrencyData(List<TpBetRecordCheck> tpBetRecordData, int mainCurrencyId, int subCurrencyId)
        {
            TpBetRecordCheck mainCurrencyRecord = tpBetRecordData.FirstOrDefault(i => i.CurrencyId == mainCurrencyId);
            TpBetRecordCheck subCurrencyRecord = tpBetRecordData.FirstOrDefault(i => i.CurrencyId == subCurrencyId);

            mainCurrencyRecord.Stake += subCurrencyRecord.Stake;
            mainCurrencyRecord.ValidBet += subCurrencyRecord.ValidBet;
            mainCurrencyRecord.WinlostAmount += subCurrencyRecord.WinlostAmount;
            mainCurrencyRecord.Revenue += subCurrencyRecord.Revenue;
            mainCurrencyRecord.ProgressiveBet += subCurrencyRecord.ProgressiveBet;
            mainCurrencyRecord.ProgressiveWin += subCurrencyRecord.ProgressiveWin;
        }

        public IEnumerable<BettransCheckData> ProcessBettransSubCurrencyToMainCurrency(IEnumerable<BettransCheckData> bettransCheckData)
        {
            var mainCurrencyId = 3;
            var subCurrencyId = 72;
            bool hasMainCurrency = bettransCheckData.Any(c => c.CurrencyId == mainCurrencyId);
            bool hasSubCurrency = bettransCheckData.Any(c => c.CurrencyId == subCurrencyId);

            if (!hasMainCurrency && hasSubCurrency)
            {
                // 無主幣別資料將子幣別取代為主幣別
                ReplaceBettransSubCurrencyWithMainCurrency(bettransCheckData, mainCurrencyId, subCurrencyId);
            }
            else if (hasMainCurrency && hasSubCurrency)
            {
                // 加總
                AggregateBettransCurrencyData(bettransCheckData, mainCurrencyId, subCurrencyId);
            }

            return bettransCheckData.Where(x => x.CurrencyId != 72);
        }

        private void ReplaceBettransSubCurrencyWithMainCurrency(IEnumerable<BettransCheckData> bettransCheckData, int mainCurrencyId, int subCurrencyId)
        {
            BettransCheckData subCurrencyRecord = bettransCheckData.FirstOrDefault(i => i.CurrencyId == subCurrencyId);
            subCurrencyRecord.CurrencyId = mainCurrencyId;
        }

        private void AggregateBettransCurrencyData(IEnumerable<BettransCheckData> bettransCheckData, int mainCurrencyId, int subCurrencyId)
        {
            BettransCheckData mainCurrencyRecord = bettransCheckData.FirstOrDefault(i => i.CurrencyId == mainCurrencyId);
            BettransCheckData subCurrencyRecord = bettransCheckData.FirstOrDefault(i => i.CurrencyId == subCurrencyId);

            mainCurrencyRecord.Stake += subCurrencyRecord.Stake;
            mainCurrencyRecord.ValidBet += subCurrencyRecord.ValidBet;
            mainCurrencyRecord.WinlostAmount += subCurrencyRecord.WinlostAmount;
            mainCurrencyRecord.Payout += subCurrencyRecord.Payout;
            mainCurrencyRecord.Revenue += subCurrencyRecord.Revenue;
            mainCurrencyRecord.ProgressiveBet += subCurrencyRecord.ProgressiveBet;
            mainCurrencyRecord.ProgressiveWin += subCurrencyRecord.ProgressiveWin;
        }
    }

}
