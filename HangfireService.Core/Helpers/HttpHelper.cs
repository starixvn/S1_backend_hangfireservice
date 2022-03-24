using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog.Context;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace HangfireService.Core.Helpers
{
    public class HttpHelper
    {
        private readonly HttpClient client;
        public static string proxyHost;
        public static int proxyPort;

        public HttpHelper(IHttpClientFactory clientFactory, IConfiguration configuration)
        {
            if (configuration.GetValue<string>("ProxyHost") != null)
            {
                client = clientFactory.CreateClient("with_proxy");
            }
            else
            {
                client = clientFactory.CreateClient();
            }
        }

        public HttpClient GetClient() => client;

        public HttpClient GetClientWithHandler(HttpClientHandler handler) => new HttpClient(handler);
        public async Task<string> HttpPostAsync(string url, Dictionary<string, string> headerParams, string parameters, string contentType)
        {
            LogContext.PushProperty("HttpHelper.Request.URL", url);
            HttpClientHandler handler = null;
            if (!string.IsNullOrEmpty(proxyHost))
            {
                handler = new HttpClientHandler()
                {
                    Proxy = new WebProxy(proxyHost, proxyPort),
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
            using var data = new StringContent(parameters, Encoding.UTF8, contentType);
            using var response = await client.PostAsync(url, data);

            string result = response.Content.ReadAsStringAsync().Result;
            return result;
        }

        public async Task<HttpResponseMessage> HttpPost1Async(string url, Dictionary<string, string> headerParams, string parameters, string contentType)
        {
            LogContext.PushProperty("HttpHelper.Request.URL", url);
            HttpClientHandler handler = null;
            if (!string.IsNullOrEmpty(proxyHost))
            {
                handler = new HttpClientHandler()
                {
                    Proxy = new WebProxy(proxyHost, proxyPort),
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
            using var data = new StringContent(parameters, Encoding.UTF8, contentType);
            var response = await client.PostAsync(url, data);
            return response;
        }
        public async Task<HttpResponseMessage> HttpFromPostAsync(string url, Dictionary<string, string> headerParams, Dictionary<string, string> parameters, string contentType)
        {
            LogContext.PushProperty("HttpHelper.Request.URL", url);
            HttpClientHandler handler = null;
            if (!string.IsNullOrEmpty(proxyHost))
            {
                handler = new HttpClientHandler()
                {
                    Proxy = new WebProxy(proxyHost, proxyPort),
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
            using var data = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(parameters) };

            var response = await client.SendAsync(data);
            return response;

        }
    }
}
