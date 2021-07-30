using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace HangfireService.Core.Models.DBs
{
    public class TpBetRecordDetail
    {
        public bool IsSuccess { get; set; }

        public int TpId { get; set; }

        public int GameType { get; set; }

        public string TransId { get; set; }

        public int CustId { get; set; }

        public string UserName { get; set; }

        public int CurrencyId { get; set; }

        public int SiteId { get; set; }

        public string TpUserName { get; set; }

        public object Detail 
        {
            get 
            {
                return JsonConvert.SerializeObject(_detail);
            }
            set
            {
                _detail = value;
            }
        }

        private object _detail;

        public TpBetRecordDetail(TpDetailTodo todo)
        {
            TpId = todo.TpId;
            GameType = todo.GameType;
            TransId = todo.TransId;
            CustId = todo.CustId;
            UserName = todo.UserName;
            CurrencyId = todo.CurrencyId;
            SiteId = todo.SiteId;
            TpUserName = todo.TpUserName;
        }
    }
}
