using Newtonsoft.Json;

namespace HangfireService.Core.Models.DBs;

public class DormantSettingDto
{
    public int SiteId { get; set; }
    public DateTime LastUpdateTime { get; set; }
    public string Setting { get; set; }
    public IEnumerable<int> Days => JsonConvert.DeserializeObject<SettingDto>(Setting).Days;

    private class SettingDto
    {
        public List<int> Days { get; set; }
    }
}

