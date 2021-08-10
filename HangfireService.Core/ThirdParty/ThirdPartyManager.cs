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

        private readonly Dictionary<int, Func<IEnumerable<TpDetailTodo>, Task<IEnumerable<bool>>>> _tpHandler;

        private readonly ReportDBDataLayer _reportDBDataLayer;

        private readonly MainDBDataLayer _mainDBDataLayer;
    
        public ThirdPartyManager(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<ThirdPartyManager>>();
            _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
            _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();

            _tpHandler = new Dictionary<int, Func<IEnumerable<TpDetailTodo>, Task<IEnumerable<bool>>>>();
            foreach (var tp in provider.GetServices<ITpHelper>())
            {
                _tpHandler.Add(tp.TpId, tp.GetTpBetRecordDetailAsync);
            }
        }

        public IEnumerable<bool> GetTpBetRecordDetailAsync(IEnumerable<TpDetailTodo> todoList)
        {
           
            var tasks = new List<Task<IEnumerable<bool>>>();
            foreach (var group in todoList.GroupBy(tp => tp.TpId))
            {
                if (_tpHandler.TryGetValue(group.Key, out var action))
                {
                    var detail = action.Invoke(group);
                    tasks.Add(detail);
                }
                else
                {
                    var update = ThirdPartyNoAction(group);
                    tasks.Add(update);
                }              
            }
            Task.WhenAll(tasks);
            var result = new List<bool>();
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

        private async Task<IEnumerable<bool>> ThirdPartyNoAction(IEnumerable<TpDetailTodo> todoList)
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

    }
}



