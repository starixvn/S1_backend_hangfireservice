using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HangfireService.Core.Models.Options
{
    public class CurrencyOption
    {
        public int CurrencyId { get; set; }
        public string TimeZone { get; set; }
        public string IsoCode { get; set; }
    }
}
