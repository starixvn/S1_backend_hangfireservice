using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HangfireService.Core.Models
{
    public class FinanceCheckResult
    {

        public int TpId { get; set; }

        public int CurrencyId { get; set; }

        public string ErrorMessage { get; set; }

    }
}
