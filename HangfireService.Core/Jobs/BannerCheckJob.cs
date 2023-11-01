using System.ComponentModel;

using Hangfire;
using Hangfire.Server;

using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Serilog.Context;

namespace HangfireService.Core.Jobs
{
    public class BannerCheckJob : BaseJob, IScheduledJob
    {
        private readonly MainDBDataLayer _mainDBDataLayer;
        private readonly ReportDBDataLayer _reportDBDataLayer;

        public BannerCheckJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
            _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");

                var todoList = await _reportDBDataLayer.GetBanner();
                var count = 0;
                foreach (var todo in todoList)
                {
                    var result = await _mainDBDataLayer.ExpiredBanner(todo.BannerName, todo.SiteId, todo.CurrencyId);
                    if (result >= 1)
                    {
                        count++;
                    }
                }

                return JsonConvert.SerializeObject(new
                {
                    Todo = todoList.Count(),
                    Success = count
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