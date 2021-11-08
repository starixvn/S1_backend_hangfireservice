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
using System.ComponentModel;
using System.Collections.Generic;

namespace HangfireService.Core.Jobs
{
    public class TpNotImplementedJob : BaseJob, IScheduledJob
    {
        private readonly ThirdPartyManager _thirdPartyManager;

        public TpNotImplementedJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _thirdPartyManager = provider.GetRequiredService<ThirdPartyManager>();
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption)
        {
            try
            {
                _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");
                var excludesTpList = jobOption.Parameters.Select(item => Convert.ToInt32(item.Value));
                var result = await _thirdPartyManager.TpNotImplemented(excludesTpList);

                return JsonConvert.SerializeObject(new
                {
                    Success = result,
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
