using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Newtonsoft.Json;
using HangfireService.Core.ThirdParty.Interfaces;
using System.ComponentModel;
using Hangfire.Server;
using Hangfire;
using HangfireService.Core.Models.Options;

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
            try
            {
                var option = JsonConvert.DeserializeObject<TpDetailOption>(JsonConvert.SerializeObject(jobOption.Parameters));
                if (!_tpHandler.TryGetValue(option.TpId, out ITpHelper tpHelper))
                {
                    throw new NotImplementedException($"{option.TpId} NotImplemented");
                }

                LogContext.PushProperty("tpId", option.TpId);
                _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");

                var todoList = await _mainDBDataLayer.GetTpDetailTodoListByTpId(option);
                var result =  await tpHelper.GetTpBetRecordDetailAsync(todoList, option);

                
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
