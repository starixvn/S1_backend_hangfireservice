using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HangfireService.Core.Models.DBs
{
    public class TpCurrency
    {
        public int CurrencyId { set; get; }
        public string TpCurrencyId { set; get; }
        public decimal ExchangeRate { set; get; }
    }
}
