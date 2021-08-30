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

        public IEnumerable<bool> TpNotImplemented(IEnumerable<TpDetailTodo> todoList, IEnumerable<int> excludesTpList)
        {
            var result = new List<bool>();
            var tasks = new List<Task<IEnumerable<bool>>>();
            foreach (var group in todoList.GroupBy(tp => tp.TpId))
            {
                if (excludesTpList.Contains(group.Key))
                {
                    result.AddRange(group.Select(item => true));
                }
                else
                {
                    var update = UpdateTpDetailTodoToSuccess(group);
                    tasks.Add(update);
                }              
            }
            Task.WhenAll(tasks);
            foreach (var task in tasks)
            {
                result.AddRange(task.Result);
            }
            return result;
        }

        public IEnumerable<ThirdPartyModel> GetAllTpList()
        {
            return _reportDBDataLayer.GetAllTpList();
        }

        private async Task<IEnumerable<bool>> UpdateTpDetailTodoToSuccess(IEnumerable<TpDetailTodo> todoList)
        {
            var result = new List<bool>();
            Parallel.ForEach(todoList, async todo =>
            {
                try
                {
                    var update = await _mainDBDataLayer.UpdateTpDetailTodo(new TpBetRecordDetail(todo) { IsSuccess = true });
                    result.Add(update);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            });
            return result;
        }

        public async Task<IEnumerable<bool>> ResetTpDetailTodo(IEnumerable<TpDetailTodo> todoList)
        {
            var count = todoList.Count();
            var result = await _mainDBDataLayer.ResetTpDetailTodo();
            return todoList.Select(item => count == result);
        }
    }
}



