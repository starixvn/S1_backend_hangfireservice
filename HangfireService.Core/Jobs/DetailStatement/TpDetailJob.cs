using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using HangfireService.Core.ThirdParty.Interfaces;
using System.Collections.Generic;
using System.ComponentModel;
using Hangfire.Server;
using Hangfire;

namespace HangfireService.Core.Jobs
{
    public class TpDetailJob : BaseJob, IScheduledJob
    {
        private readonly MainDBDataLayer _mainDBDataLayer;

        private readonly Dictionary<int, ITpHelper> _tpHandler = new Dictionary<int, ITpHelper>();

        public TpDetailJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();

            foreach (var tp in provider.GetServices<ITpHelper>())
            {
                _tpHandler.Add(tp.TpId, tp);
            }
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            var jobId = context.BackgroundJob.Id;
            LogContext.PushProperty("jobId", jobId);

            try
            {              
                int tpId = Convert.ToInt32(jobOption.Parameters.First().Value);
                if (!_tpHandler.TryGetValue(tpId, out ITpHelper tpHelper))
                {
                    throw new NotImplementedException($"{tpId} NotImplemented");
                }

                LogContext.PushProperty("tpId", tpId);
                _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} {jobId} running......");

                var todoList = await _mainDBDataLayer.GetTpDetailTodoListByTpId(tpId);
                var result =  await tpHelper.GetTpBetRecordDetailAsync(todoList);

                return JsonConvert.SerializeObject(new
                {
                    Todo = todoList.Distinct(p => new { p.TpId, p.GameType, p.TransId }).Count(),
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
