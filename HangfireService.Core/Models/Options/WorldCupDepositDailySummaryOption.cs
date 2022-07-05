using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HangfireService.Core.Models.Options
{
    public class WorldCupDepositDailySummaryOption
    {
        public int SiteId { get; set; }

        public int CurrencyId { get; set; }

        public int CustId { get; set; }

        public DateTime? AccountDate { get; set; }
    }
}
