using Hangfire;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using Hangfire.Server;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.ThirdParty;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("DetailStatement")]
    public class DetailStatementJob : IScheduledJob
    {
        public string ClassName { get; set; }

        private readonly ILogger<DetailStatementJob> _logger;

        private readonly MainDBDataLayer _mainDBDataLayer;

        private readonly ThirdPartyManager _thirdPartyManager;

        public DetailStatementJob(IServiceProvider provider)
        {
            ClassName = GetType().Name;
            _logger = provider.GetRequiredService<ILogger<DetailStatementJob>>();
            _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
            _thirdPartyManager = provider.GetRequiredService<ThirdPartyManager>();
        }

        public async Task Execute(JobDetail jobDetail, string[] parameters)
        {
            var jobGuid = Guid.NewGuid();
            LogContext.PushProperty("JobGuid", jobGuid);
            LogContext.PushProperty("Job", jobDetail.JobName);
            try
            {
                _logger.LogInformation($"{jobGuid} || {DateTime.Now:HH:mm:ss} || {jobDetail.JobName} running......");

                var status = int.Parse(parameters?[0] ?? "0");
                var todoList = await _mainDBDataLayer.GetTpDetailTodoList(status);
                var result = await _thirdPartyManager.GetTpBetRecordDetailAsync(todoList);

                _logger.LogInformation($"Todo: {todoList.Count()} || Success: {result.Count(i => i)} || Fail: {result.Count(i => !i)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        [Job]
        [DisplayName("呼叫內部方法")]
        public void Action(PerformContext context = null, IJobCancellationToken cancellationToken = null)
        {
            if (cancellationToken.ShutdownToken.IsCancellationRequested)
            {
                return;
            }

            _ = Execute(null, null);
        }

    }
}
