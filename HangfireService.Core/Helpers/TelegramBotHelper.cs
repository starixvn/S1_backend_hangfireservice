using System.Text;

using HangfireService.Core.Models.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

namespace HangfireService.Core.Helpers
{
    public class TelegramBotHelper
    {
        protected readonly ILogger<TelegramBotHelper> _logger;
        protected readonly HttpHelper _httpHelper;
        private readonly TelegramConfig _telegramConfig;
        private readonly TelegramConfig _telegramClientConfig;

        public TelegramBotHelper(IServiceProvider provider)
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            _logger = provider.GetRequiredService<ILogger<TelegramBotHelper>>();
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _telegramConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<TelegramConfig>("HangfireTelegram");
            _telegramClientConfig = configuration.GetSection("HangfireTelegramClient").Get<TelegramConfig>();
        }

        public void SendMessage(string message, bool toClient = false)
        {
            var config = toClient ? _telegramClientConfig : _telegramConfig;

            if (!config.Enable)
            {
                return;
            }

            // telegrm not allow more than 30 messages per second, not be able to send more than 20 messages per minute to the same group
            Thread.Sleep(6000);

            var url = $"{config.Url}/bot{config.Token}/sendMessage";
            var param = new
            {
                chat_id = config.ChatId,
                text = message
            };

            _ = HttpPostAsync(url, JsonConvert.SerializeObject(param));
        }

        public void GetUpdates()
        {
            var url = $"{_telegramConfig.Url}/bot{_telegramConfig.Token}/getUpdates";

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
