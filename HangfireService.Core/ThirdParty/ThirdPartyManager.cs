using HangfireService.Core.DataLayers;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.ThirdParty.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HangfireService.Core.ThirdParty
{
    public class ThirdPartyManager
    {
        private readonly ILogger<ThirdPartyManager> _logger;

        private readonly ReportDBDataLayer _reportDBDataLayer;

        private readonly MainDBDataLayer _mainDBDataLayer;
    
        public ThirdPartyManager(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<ThirdPartyManager>>();
            _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
            _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
        }

        public Task<int> TpNotImplemented(IEnumerable<string> excludesTpList)
        {
            return DeleteTpNotImplemented(excludesTpList);
        }

        public IEnumerable<ThirdPartyModel> GetAllTpList()
        {
            return _reportDBDataLayer.GetAllTpList();
        }

        private async Task<int> DeleteTpNotImplemented(IEnumerable<string> tpIds)
        {      
            return await _mainDBDataLayer.DeleteTpNotImplemented(tpIds);
        }

        public async Task<IEnumerable<bool>> ResetTpDetailTodo(IEnumerable<TpDetailTodo> todoList)
        {
            var count = todoList.Count();
            var result = await _mainDBDataLayer.ResetTpDetailTodo();
            return todoList.Select(item => count == result);
        }
    }
}



