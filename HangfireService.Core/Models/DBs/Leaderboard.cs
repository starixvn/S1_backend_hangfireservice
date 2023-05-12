using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HangfireService.Core.Models.DBs
{
    public class Leaderboard
    {
        public int CustId { get; set; }
        public string UserName { get; set; }
        public decimal WTurnover { get; set; }
        public int Status { get; set; }
    }
}
