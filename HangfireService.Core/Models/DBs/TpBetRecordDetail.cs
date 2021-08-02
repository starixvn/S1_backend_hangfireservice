using Newtonsoft.Json;

namespace HangfireService.Core.Models.DBs
{
    public class TpBetRecordDetail
    {
        public bool IsSuccess { get; set; }

        public int TpId { get; set; }

        public int GameType { get; set; }

        public string TransId { get; set; }

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
        }
    }
}
