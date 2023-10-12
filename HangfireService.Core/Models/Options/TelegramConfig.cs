namespace HangfireService.Core.Models.Options
{
    public class TelegramConfig
    {
        public string Url { get; set; }
        public string Token { get; set; }
        public int ChatId { get; set; }

        public bool Enable { get; set; }
    }
}
