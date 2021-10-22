using System;
using Newtonsoft.Json;

namespace HangfireService.Core.Models.DBs
{
    public class TpBetRecordDetail
    {
        public bool IsSuccess { get; set; }

        public int TpId { get; set; }

        public int GameType { get; set; }

        public int CurrencyId { get; set; }

        public string TransId { get; set; }

        public DateTime SettledTime { get; set; }

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
            CurrencyId = todo.CurrencyId;
            SettledTime = todo.SettledTime > DateTime.UtcNow.AddMonths(1) ? DateTime.UtcNow : todo.SettledTime; //體育結算日期會超過下個月改用現在日期
        }
    }
}
