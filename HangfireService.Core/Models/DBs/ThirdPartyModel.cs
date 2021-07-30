using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace HangfireService.Core.Models.DBs
{
    public class ThirdPartyModel
    {
        public int SiteId { set; get; }
        public int TpId { set; get; }
        public string TpName { set; get; }
        public string Setting { set; get; }
        public Dictionary<string, string> TpSetting => JsonConvert.DeserializeObject<Dictionary<string, string>>(Setting);
    }
}
