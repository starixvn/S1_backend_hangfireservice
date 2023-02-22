using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Extensions;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Newtonsoft.Json;
using HangfireService.Core.ThirdParty.Interfaces;
using System.ComponentModel;
using Hangfire.Server;
using Hangfire;
using HangfireService.Core.Models.Options;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using Hangfire.Dashboard.Management.Pages;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("TpDetailJob", queue: "default")]
    public class TpDetailJob : BaseJob, IScheduledJob
    {
        private readonly MainDBDataLayer _mainDBDataLayer;

        private readonly Dictionary<int, IDetailTpHelper> _tpHandler = new();

        private static List<JobOption> _tpDetailJobList;

        public TpDetailJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
            MyJobManager.GetJobGroups()?.TryGetValue("Default", out _tpDetailJobList);

            foreach (var tp in provider.GetServices<IDetailTpHelper>())
            {
                _tpHandler.Add(tp.TpId, tp);
            }
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            // 處理透過web介面手動新增的RecurringJob名稱
            if (context.BackgroundJob.Job.ToString().Equals("TpDetailJob.Run", StringComparison.OrdinalIgnoreCase))
            {
                jobName = context.Connection.GetRecurringJobs().First(item => item.LastJobId == context.BackgroundJob.Id).Id;
            }

            //避免執行太久拿到重複資料，每次進來先暫停Job狀態
            MyJobManager.Pause(jobName);        
            try
            {
                var option = JsonConvert.DeserializeObject<TpDetailOption>(jobOption.Parameters);
                if (!_tpHandler.TryGetValue(option.TpId, out IDetailTpHelper tpHelper))
                {
                    throw new NotImplementedException($"{option.TpId} NotImplemented");
                }

                LogContext.PushProperty("tpId", option.TpId);
                _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");

                var todoList = await _mainDBDataLayer.GetTpDetailTodoListByTpId(option);
                var result = await tpHelper.GetTpBetRecordDetailAsync(todoList, option);

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
            finally
            {
                //重新啟動Job
                MyJobManager.Start(jobName);
            }
        }

        [Job]
        [DisplayName("Run {2} by ManagementPage")]
        public object Run(PerformContext context = null, IJobCancellationToken cancellationToken = null,
            [DisplayData("JobName", ConvertType = typeof(TpDetailJobList))]string jobName = null,
            [DisplayData("LiminNum", defaultValue:"1000")]string liminNum = "1000",
            [DisplayData("TaskCount", defaultValue:"4")]string taskCount = "4")
        {
            if (!_tpDetailJobList.Any())
            {
                return null;
            }

            var jobOption = _tpDetailJobList.First(i => i.JobName == jobName);
            jobOption.Parameters = jobOption.Parameters.Update("LiminNum", liminNum);
            jobOption.Parameters = jobOption.Parameters.Update("TaskCount", taskCount);

            return ExecuteAsync($"Execute.{jobName}.({liminNum}/{taskCount})", jobOption, context, cancellationToken).Result;
        }

        private class TpDetailJobList : IInputDataList
        {
            public Dictionary<string, string> GetData()
            {
                return _tpDetailJobList?.Where(i => i.ClassName == "TpDetailJob").ToDictionary(f => f.JobName, f => f.JobName);
            }

            public string GetDefaultValue()
            {
                return null;
            }
        }
    }
}
