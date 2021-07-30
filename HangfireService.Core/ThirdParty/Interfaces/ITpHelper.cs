using HangfireService.Core.Models.DBs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace HangfireService.Core.ThirdParty.Interfaces
{
    public interface ITpHelper
    {
        int TpId { get;}

        Task<IEnumerable<bool>> GetTpBetRecordDetailAsync(IEnumerable<TpDetailTodo> todoList);
    }
}
