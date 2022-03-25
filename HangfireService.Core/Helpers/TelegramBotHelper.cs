using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace HangfireService.Core.Helpers
{
    public class TelegramBotHelper
    {

        protected readonly ILogger<TelegramBotHelper> _logger;

        protected readonly HttpHelper _httpHelper;

        private static IReadOnlyDictionary<string, dynamic>? _telegram;
        private readonly int _telegramNotify;


        public TelegramBotHelper(IServiceProvider provider, IConfiguration configuration)
        {
            _logger = provider.GetRequiredService<ILogger<TelegramBotHelper>>();
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _telegramNotify = configuration.GetValue<int>("TelegramNotify");
            _telegram = configuration.GetSection("Telegram").Get<Dictionary<string, dynamic>>();
        }

        public void SendMessage(string message)
        {
            if (_telegramNotify != 1)
            {
                return;
            }

            var url = $"{_telegram["Url"]}/bot{_telegram["Token"]}/sendMessage";
            var param = new
            {
                chat_id = _telegram["ChatId"],
                text = message
            };

            _ = HttpPostAsync(url, JsonConvert.SerializeObject(param));
        }

        public void GetUpdates()
        {
            var url = $"{_telegram["Url"]}/bot{_telegram["Token"]}/getUpdates";

            _ = HttpPostAsync(url, "");
        }

        protected async Task<HttpResponseMessage> HttpPostAsync(string url, string parameters)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(parameters, Encoding.UTF8, "application/json");

                var response = await _httpHelper.GetClient().SendAsync(request);
                response.EnsureSuccessStatusCode();
                return response;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
