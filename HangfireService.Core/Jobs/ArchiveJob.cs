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
using HangfireService.Core.Models.Options;
using System.Globalization;
using System.Diagnostics;
using System.Threading;

namespace HangfireService.Core.Jobs
{
    public class ArchiveJob : BaseJob, IScheduledJob
    {
        private readonly MainDBDataLayer _mainDBDataLayer;

        private readonly ReportDBDataLayer _reportDBDataLayer;

        private readonly ArchiveDBDataLayer _archiveDBDataLayer;

        public ArchiveJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
            _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
            _archiveDBDataLayer = provider.GetRequiredService<ArchiveDBDataLayer>();
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption)
        {
            var result = 0;
            var beginAt = GetUTC8Time();
            var today = beginAt.Date.ToString(CultureInfo.InvariantCulture);

            try
            {
                var option = JsonConvert.DeserializeObject<ArchiveOption>(JsonConvert.SerializeObject(jobOption.Parameters));

                var data = _reportDBDataLayer.GetArchiveData(option.GetSP, option.LiminNum);

                Parallel.ForEach(data, item =>
                {
                    var insertResult = _archiveDBDataLayer.InsertArchiveData(option.InsertSP, item);
                    if (insertResult > 0)
                    {
                        result += _mainDBDataLayer.DeleteArchiveData(option.DeleteSP, item);
                    }

                });
                
                return JsonConvert.SerializeObject(new
                {
                    Success = result,
                    data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return JsonConvert.SerializeObject(ex);
            }      
        }

        private DateTime GetUTC8Time()
        {
            return new DateTimeOffset(DateTime.Now).ToOffset(new TimeSpan(8, 0, 0)).DateTime;
        }
    }
}
