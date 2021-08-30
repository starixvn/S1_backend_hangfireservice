using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.ThirdParty;
using HangfireService.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HangfireService.Core.Jobs
{
    public class TodoStatusResetJob : BaseJob, IScheduledJob
    {
        private readonly MainDBDataLayer _mainDBDataLayer;

        private readonly ThirdPartyManager _thirdPartyManager;

        public TodoStatusResetJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
            _thirdPartyManager = provider.GetRequiredService<ThirdPartyManager>();
        }

        public async Task<object> ExecuteAsync(JobDetail jobDetail)
        {
            try
            {
                _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobDetail.JobName} running......");
  
                var todoList = await _mainDBDataLayer.GetTpDetailTodoListByStatus(-1);
                var result =  await _thirdPartyManager.ResetTpDetailTodo(todoList);

                return JsonConvert.SerializeObject(new
                {
                    Todo = todoList.Count(),
                    Success = result.Count(i => i),
                    Fail = result.Count(i => !i),
                });
            }
            catch (Exception ex)
            {            
                _logger.LogError(ex, ex.Message);
                return JsonConvert.SerializeObject(ex);
            }
          
        }
    }
}
