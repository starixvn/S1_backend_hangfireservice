using System;
using System.Collections.Generic;
using System.Text;

namespace HangfireService.Core.Models.DBs
{
    public class TpDetailTodo
    {
        public int TpId { get; set; }

        public int GameType { get; set; }

        public string TransId { get; set; }

        public int CustId { get; set; }

        public string UserName { get; set; }

        public int CurrencyId { get; set; }

        public int SiteId { get; set; }

        public string TpUserName { get; set; }
    }
}
