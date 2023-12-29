using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using HangfireService.Core.Models.Options;

using Microsoft.Extensions.Logging;

using Serilog.Context;

namespace HangfireService.Core.Helpers
{
    public class HttpHelper
    {
        private readonly HttpClient _client;
        private readonly ILogger<HttpHelper> _logger;
        private readonly string _proxyHost;
        private readonly int _proxyPort;

        public HttpHelper(IServiceProvider provider)
        {
            var clientFactory = provider.GetRequiredService<IHttpClientFactory>();
            _logger = provider.GetRequiredService<ILogger<HttpHelper>>();
            var proxyConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<ProxyConfig>("ProxySetting");

            _proxyHost = proxyConfig.ProxyHost;
            _proxyPort = proxyConfig.ProxyPort;

            _client = _proxyHost != null
                ? clientFactory.CreateClient("with_proxy")
                : clientFactory.CreateClient();
        }

        public HttpClient GetClient() => _client;

        public HttpClient GetClientWithHandler(HttpClientHandler handler) => new HttpClient(handler);

        public async Task<string> HttpPostAsync(string url, Dictionary<string, string> headerParams, string parameters, string contentType)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("HttpHelper.Request.URL", url);
            HttpClientHandler handler = null;
            if (!string.IsNullOrEmpty(_proxyHost))
            {
                handler = new HttpClientHandler()
                {
                    Proxy = new WebProxy(_proxyHost, _proxyPort),
                    UseProxy = true,
                };
            }
            else
            {
                handler = new HttpClientHandler();
            }

            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));
            foreach (var param in headerParams)
            {
                client.DefaultRequestHeaders.Add(param.Key, param.Value);
            }

            _logger.LogInformation($"HttpHelper.Request.BODY:{parameters}");

            using var data = new StringContent(parameters, Encoding.UTF8, contentType);
            using var response = await client.PostAsync(url, data);

            var result = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"HttpHelper.Request.Response:{result}");
            return result;
        }

        public async Task<HttpResponseMessage> HttpPost1Async(string url, Dictionary<string, string> headerParams, string parameters, string contentType)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("HttpHelper.Request.URL", url);
            HttpClientHandler handler = null;
            if (!string.IsNullOrEmpty(_proxyHost))
            {
                handler = new HttpClientHandler()
                {
                    Proxy = new WebProxy(_proxyHost, _proxyPort),
                    UseProxy = true,
                };
            }
            else
            {
                handler = new HttpClientHandler();
            }

            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));
            if (headerParams != null)
            {
                foreach (var param in headerParams)
                {
                    client.DefaultRequestHeaders.Add(param.Key, param.Value);
                }
            }

            _logger.LogInformation($"HttpHelper.Request.BODY:{parameters}");

            using var data = new StringContent(parameters, Encoding.UTF8, contentType);
            var response = await client.PostAsync(url, data);
            var result = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"HttpHelper.Request.Response:{result}");
            return response;
        }

        public async Task<HttpResponseMessage> HttpFromPostAsync(string url, Dictionary<string, string> headerParams, Dictionary<string, string> parameters, string contentType)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("HttpHelper.Request.URL", url);
            HttpClientHandler handler = null;
            if (!string.IsNullOrEmpty(_proxyHost))
            {
                handler = new HttpClientHandler()
                {
                    Proxy = new WebProxy(_proxyHost, _proxyPort),
                    UseProxy = true,
                };
            }
            else
            {
                handler = new HttpClientHandler();
            }

            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));
            if (headerParams != null)
            {
                foreach (var param in headerParams)
                {
                    client.DefaultRequestHeaders.Add(param.Key, param.Value);
                }
            }

            _logger.LogInformation($"HttpHelper.Request.BODY:{string.Join(",", parameters.Select(i => $"{i.Key}={i.Value}"))}");

            using var data = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(parameters) };
            var response = await client.SendAsync(data);
            var result = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"HttpHelper.Request.Response:{result}");
            return response;

        }
        
        public async Task<HttpResponseMessage> HttpGetAsync(string url, Dictionary<string, string> headerParams, string contentType)
        {
            HttpClientHandler handler;
            if (!string.IsNullOrEmpty(_proxyHost))
            {
                handler = new HttpClientHandler
                {
                    Proxy = new WebProxy(_proxyHost, _proxyPort),
                    UseProxy = true,
                };
            }
            else
            {
                handler = new HttpClientHandler();
            }

            using var client = new HttpClient(handler);
            if (contentType != null)
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));
            }

            if (headerParams != null)
            {
                foreach (var param in headerParams)
                {
                    client.DefaultRequestHeaders.Add(param.Key, param.Value);
                }
            }

            LogContext.PushProperty("HttpHelper.Request.URL", url);
            using var data = new HttpRequestMessage(HttpMethod.Get, url);
            data.Content = new StringContent("", Encoding.UTF8, contentType);
            var response = await client.SendAsync(data);
            _logger.LogInformation($"HttpHelper.Request.Response:{response.Content.ReadAsStringAsync().Result}");
            return response;
        }
        
    }
}
