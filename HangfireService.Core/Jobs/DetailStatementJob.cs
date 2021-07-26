using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
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
    public class DetailStatementJob : IScheduledJob
    {
        private const string Name = "DetailStatementJob";

        private readonly ILogger<DetailStatementJob> _logger;

        private readonly MainDBDataLayer _mainDBDataLayer;

        public DetailStatementJob(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<DetailStatementJob>>();
            _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
            JobDetailOption = provider.GetRequiredService<List<JobDetailOption>>().First(item => item.Name == Name);
        }

        public JobDetailOption JobDetailOption { get; set; }

        public void Execute()
        {
            var jobGuid = Guid.NewGuid();
            LogContext.PushProperty("JobGuid", jobGuid);
            LogContext.PushProperty("Job", "DetailStatementJob");
            try
            {
                _logger.LogInformation($"{jobGuid} || {DateTime.Now:HH:mm:ss} || {Name} running......");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

    }
}
