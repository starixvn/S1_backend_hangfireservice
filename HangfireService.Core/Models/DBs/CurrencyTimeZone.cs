using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HangfireService.Core.Models.DBs
{
    public class CurrencyTimeZone
    {
        public int CurrencyId { get; set; }
        public string IsoCode { get; set; }
        public string TimeZone { get; set; }
    }
}
