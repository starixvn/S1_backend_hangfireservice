using Newtonsoft.Json;

namespace HangfireService.Core.Models.DBs
{
    public class ServerInfo
    {
        public int SiteId { set; get; }
        public string SiteName { set; get; }
        public string LangList { set; get; }
        public string CurrencyList { set; get; }
        public string DefLang { set; get; }
        public int DefCurrency { set; get; }
        public IEnumerable<int> ICurrencyList => JsonConvert.DeserializeObject<IEnumerable<int>>(CurrencyList);
    }
}
