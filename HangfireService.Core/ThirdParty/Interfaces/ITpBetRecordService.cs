using HangfireService.Core.Models.DBs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace HangfireService.Core.ThirdParty.Interfaces
{
    public interface ITpBetRecordService
    {
        int TpId { get;}

        Task<TpBetRecordDetail> GetTpBetRecordDetail(TpDetailTodo tpDetailTodo);
    }
}
