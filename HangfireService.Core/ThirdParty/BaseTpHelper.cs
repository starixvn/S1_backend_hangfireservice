using System.Globalization;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Helpers;
using HangfireService.Core.Models.DBs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using Serilog.Context;

namespace HangfireService.Core.ThirdParty
{
    public class BaseTpHelper
    {
        protected readonly ILogger<BaseTpHelper> _logger;

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

        protected async Task<bool> AddTpBetRecordDetail(TpBetRecordDetail detail)
        {
            return await _mainDBDataLayer.AddTpBetRecordDetail(detail);
        }

        protected async Task<bool> UpdateTpDetailTodo(TpBetRecordDetail detail)
        {
            return await _mainDBDataLayer.UpdateTpDetailTodo(detail);
        }

        public async Task<HttpResponseMessage> HttpPostAsync(string url, byte[] data, string name, string contentType)
        {
            try
            {
                _logger.LogWarning($"Tp.Request.URL:{url}");
                LogContext.PushProperty("Tp.Request.URL", url);
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
    }

}
