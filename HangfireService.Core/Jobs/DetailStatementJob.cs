using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.ThirdParty;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HangfireService.Core.Jobs
{
    public class DetailStatementJob : BaseJob, IScheduledJob
    {
        private readonly MainDBDataLayer _mainDBDataLayer;

        private readonly ThirdPartyManager _thirdPartyManager;

        public DetailStatementJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
            _thirdPartyManager = provider.GetRequiredService<ThirdPartyManager>();
        }

        public async Task Execute(JobDetail jobDetail)
        {
            var jobGuid = Guid.NewGuid();
            LogContext.PushProperty("JobGuid", jobGuid);
            LogContext.PushProperty("JobName", jobDetail.JobName);
            try
            {
                _logger.LogInformation($"{jobGuid} || {DateTime.Now:HH:mm:ss} || {jobDetail.JobName} running......");

                var todoList = await _mainDBDataLayer.GetTpDetailTodoList(Status: jobDetail.Parameters?[0] ?? "0");
                var result = await _thirdPartyManager.GetTpBetRecordDetailAsync(todoList);

                _logger.LogInformation($"Todo: {todoList.Count()} || Success: {result.Count(i => i)} || Fail: {result.Count(i => !i)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }      
    }
}
