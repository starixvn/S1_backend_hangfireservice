using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
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

        public byte[] BinaryDetail
        {
            get
            {
                return GZipCompress();
            }
        }

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

        private byte[] GZipCompress()
        {
            var data = JsonConvert.SerializeObject(_detail);
            using var msi = new MemoryStream(Encoding.UTF8.GetBytes(data));
            using var mso = new MemoryStream();
            using (var gs = new GZipStream(mso, CompressionMode.Compress))
            {
                msi.CopyTo(gs);
            }
            return mso.ToArray();
        }

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
