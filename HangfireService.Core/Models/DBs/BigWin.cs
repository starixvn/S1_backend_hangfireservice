using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Agreement.JPake;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HangfireService.Core.Models.DBs
{
    public class BigWin
    {
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
        public int CustId { get; set; }
        public int GameType { get; set; }
        public int TpId { get; set; }
        public int CategoryId { get; set; }
        public string GameID { get; set; }
        public decimal ValidBet { get; set; }
        public decimal WinlostAmount { get; set; }
    }
    public class BigWinSetting
    {
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
        public string TpCategory { get; set; }
        public int MultipleOfWinning { get; set; }
        public int Status { get; set; }
        public string CurrencyName { get; set; }
        public string SiteName { get; set; }
        public IEnumerable<int> ITpCategoryList
        {
            get
            {
                return string.IsNullOrWhiteSpace(TpCategory) ?
                    null :
                    JsonConvert.DeserializeObject<IEnumerable<int>>(TpCategory);
            }
        }
        private DateTimeOffset _settleTime { get; set; }
        public DateTimeOffset SettleTime
        {
            get => _settleTime;
            set => _settleTime = new DateTimeOffset(value.DateTime, TimeSpan.Zero);
        }
    }

    public class BigWinInfo
    {
        public int SiteId { get; set; }
        public int CustId { get; set; }
        public int CurrencyId { get; set; }
        public int TpId { get; set; }
        public int TpCategory { get; set; }
        public int TpGameType { get; set; }
        public string GameID { get; set; }
        public DateTime Time { get; set; }
    }
}
