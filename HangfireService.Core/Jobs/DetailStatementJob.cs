using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.ThirdParty;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace HangfireService.Core.Jobs
{
    public class DetailStatementJob : IScheduledJob
    {
        private const string Name = "DetailStatementJob";

        private readonly ILogger<DetailStatementJob> _logger;

        private readonly MainDBDataLayer _mainDBDataLayer;

        private readonly ThirdPartyManager _thirdPartyManager;

        public DetailStatementJob(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<DetailStatementJob>>();
            _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
            JobDetailOption = provider.GetRequiredService<List<JobDetailOption>>().First(item => item.Name == Name);
            _thirdPartyManager = provider.GetRequiredService<ThirdPartyManager>();
        }

        public JobDetailOption JobDetailOption { get; set; }

        public async Task Execute()
        {
            var jobGuid = Guid.NewGuid();
            LogContext.PushProperty("JobGuid", jobGuid);
            LogContext.PushProperty("Job", "DetailStatementJob");
            try
            {
                _logger.LogInformation($"{jobGuid} || {DateTime.Now:HH:mm:ss} || {Name} running......");

                var todoList = await _mainDBDataLayer.GetTpDetailTodoList();
                var result = new List<bool>();    
                Parallel.ForEach(todoList, async item =>
                {
                    try
                    {                 
                        var detailresult = await _thirdPartyManager.GetTpBetRecordDetail(item);
                        result.Add(detailresult);
                    }
                    catch (Exception e )
                    {
                        _logger.LogError(e, e.Message);
                    }
                });

                _logger.LogInformation($"Todo: {todoList.Count()} || Success: {result.Count(i => i)} || Fail: {result.Count(i => !i)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

    }
}
