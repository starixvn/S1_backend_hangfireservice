using HangfireService.Core.DataLayers;
using HangfireService.Core.Helpers;
using HangfireService.Core.Models.DBs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

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
