using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HangfireService.Core.Helpers
{
    public class HttpHelper
    {
        private readonly HttpClient client;

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
    }
}
