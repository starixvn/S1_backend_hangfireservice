using HangfireService.Core.DataLayers;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.ThirdParty.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace HangfireService.Core.ThirdParty
{
    public class ThirdPartyManager
    {
        private readonly Dictionary<int, Func<TpDetailTodo, Task<TpBetRecordDetail>>> _tpHandler = new Dictionary<int, Func<TpDetailTodo, Task<TpBetRecordDetail>>>();

        private readonly WagerDBDataLayer _wagerDBDataLayer;

        public ThirdPartyManager(IServiceProvider provider)
        {
            _wagerDBDataLayer = provider.GetRequiredService<WagerDBDataLayer>();

            foreach (var tp in provider.GetServices<ITpBetRecordService>())
            {
                _tpHandler.Add(tp.TpId, tp.GetTpBetRecordDetail);
            }
        }
        public async Task<bool> GetTpBetRecordDetail(TpDetailTodo tpDetailTodo)
        {
            if (!_tpHandler.TryGetValue(tpDetailTodo.TpId, out var action))
            {
                return true;
            }

            var tpReuslt = await action.Invoke(tpDetailTodo);            
            if (!tpReuslt.IsSuccess)
            {
                return tpReuslt.IsSuccess;
            }

            var addResult = await _wagerDBDataLayer.AddTpBetRecordDetail(tpReuslt);
            return addResult;
        } 
    }
}


