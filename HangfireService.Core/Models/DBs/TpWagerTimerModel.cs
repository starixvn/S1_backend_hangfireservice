using Newtonsoft.Json;

namespace HangfireService.Core.Models.DBs
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class TpWagerTimerModel
    {
        public int TpId { get; set; }

        public int Type { get; set; }

        public DateTime StartTime { get; set; }

        public string? Currency { get; set; }

        public string? Reference { get; set; }

        public int? GameType { get; set; }
    }
}