using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HangfireService.Core.Models.Finance
{
    public class CheckData
    {
        public string TpCurrencyId { get; set; }

        public int CurrencyId { get; set; }
        
        public decimal Stake { get; set; }

        public decimal ValidBet { get; set; }

        public decimal Payout { get; set; }

        public decimal WinlostAmount { get; set; }

        public decimal Revenue { get; set; }

        public decimal ProgressiveBet { get; set; }

        public decimal ProgressiveWin { get; set; }

    }
}
