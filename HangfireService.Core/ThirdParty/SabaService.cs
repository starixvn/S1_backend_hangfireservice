using HangfireService.Core.Models.DBs;
using HangfireService.Core.ThirdParty.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace HangfireService.Core.ThirdParty
{
    public class SabaService : ITpBetRecordService
    {
        int ITpBetRecordService.TpId => 1;

        public SabaService(IServiceProvider provider)
        { 
        
        }

        public async Task<TpBetRecordDetail> GetTpBetRecordDetail(TpDetailTodo tpDetailTodo)
        {
            return new TpBetRecordDetail()
            {
                IsSuccess = true,
                TpId = tpDetailTodo.TpId,
                GameType = tpDetailTodo.GameType,
                TransId = tpDetailTodo.TransId,
                Detail = new { test = "saba - test"}
            };
        }
    }

}
