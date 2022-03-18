using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;

namespace HangfireService.Core.Models.DBs
{
    public class TpBetRecordCheck
    {
        public int TpId { get; set; }

        public int CurrencyId { get; set; }

        public decimal Stake { get; set; }

        public decimal ValidBet { get; set; }

        public decimal WinlostAmount { get; set; }
    }
}
