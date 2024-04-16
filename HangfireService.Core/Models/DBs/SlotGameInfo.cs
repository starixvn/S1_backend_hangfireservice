using Newtonsoft.Json;

namespace HangfireService.Core.Models.DBs
{
    public class SlotGameInfo
    {
        public string SlotGameId { get; set; }
        public int TpId { get; set; }
        public string GameId { get; set; }
        public string AppId { get; set; }
        public DateTime LaunchDate { get; set; }
        public string Reels { get; set; }
        public string RTP { get; set; }
        public string Paylines { get; set; }
        public bool PlayFree { get; set; }
        public Dictionary<string, string> GameName
        {
            get
            {
                if (_gameName == null)
                {
                    return string.IsNullOrEmpty(Name) ? default : JsonConvert.DeserializeObject<Dictionary<string, string>>(Name);
                }
                return _gameName;
            }
            set
            {
                _gameName = value;
            }
        }
        private Dictionary<string, string> _gameName { get; set; }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string Name { get; set; }

        public IEnumerable<int> SlotGameCategoryList
        {
            get
            {
                if (_slotGameCategoryList == null)
                {
                    return string.IsNullOrEmpty(SlotGameCategory) ? default : JsonConvert.DeserializeObject<IEnumerable<int>>(SlotGameCategory);
                }
                return _slotGameCategoryList;
            }
            set
            {
                _slotGameCategoryList = value;
            }
        }
        private IEnumerable<int> _slotGameCategoryList { get; set; }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string SlotGameCategory { get; set; }

        public Dictionary<string, string> ImgName
        {
            get
            {
                if (GameName != null)
                {
                    var imgName = GameName.ContainsKey("en") ? GameName["en"] : string.Empty;
                    return GameName.ToDictionary(x => x.Key, x => $"{OrgFileNameFilter($"{imgName}-{x.Key}")}.jpg");
                }
                return default;
            }
        }
        private static string OrgFileNameFilter(string fileName)
        {
            return fileName.Replace(" ", "-")
                                       .Replace("&", "-")
                                       .Replace(":", "-")
                                       .Replace("'", "")
                                       .Replace("’", "")
                                       .Replace(".", "")
                                       .Replace("™", "")
                                       .Replace("---", "-")
                                       .Replace("--", "-")
                                       .Replace("\u200B", "")
                                       .Replace(",", "")
                                       .Replace("?", "")
                                       .Replace("!", "")
                                       .ToLower();
        }
        // 指定TpId排序
        public int Sort { get; set; } = 99999;
        // 最外層不分TpId排序
        public int Top { get; set; } = 99999;
    }
}
