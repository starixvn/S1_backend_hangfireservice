using Hangfire;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using Hangfire.Server;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System.ComponentModel;
using HangfireService.Core.Extensions;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Promotion;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("BonusSummaryJob", queue: "relay")]
    public class BonusSummaryJob : BaseJob, IScheduledJob
    {
        private readonly MainDBDataLayer _mainDbDataLayer;
        private readonly ReportDBDataLayer _reportDbDataLayer;
        private static List<JobOption> _jobList;
        private readonly IEnumerable<PromotionSummaryBase> _promotion;
        private readonly IBonusSummaryTimerService _timerService;

        public BonusSummaryJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            MyJobManager.GetJobGroups()?.TryGetValue("Relay", out _jobList);
            _mainDbDataLayer = provider.GetRequiredService<MainDBDataLayer>();
            _reportDbDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
            _promotion = provider.GetServices<PromotionSummaryBase>();
            _timerService = provider.GetService<IBonusSummaryTimerService>();
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");
                
                var option = JsonConvert.DeserializeObject<BonusSummaryOption>(JsonConvert.SerializeObject(jobOption.Parameters));
                
                if (option.WorkerType is WorkerType.Referral or WorkerType.ReferralFreeCredit or WorkerType.VipUpgrade or WorkerType.Other)
                {
                    var promotion = _promotion.Single(a => a.WorkerType == option.WorkerType);
                    return await promotion.Run();
                }

                #region Init
                    
                var workerTime = new WorkerTime(); 
                var workerId = 0;
                var maxId = 0;
                bool isAutoRun;
                var result = false;
                    
                #endregion
                
                if (option.TimerType == TimerType.Time)
                {
                    workerTime = GetWorkerTime(option , out isAutoRun);
                }
                else
                {
                    workerId = await GetWorkerId(option , out isAutoRun);
                }
                    
                await using var conn = _mainDbDataLayer.GetMySqlConnection();
                conn.Open();
                var trans = await conn.BeginTransactionAsync();
                    
                switch (option.WorkerType)
                {
                    case WorkerType.Promotion:
                        result = _mainDbDataLayer.BonusSummaryPromotionSet(workerTime.StartTime, workerTime.EndDate ,trans ) >= 0;
                        break;
                    case WorkerType.Rebate:
                        result = _mainDbDataLayer.BonusSummaryRebateSet(workerId ,trans ,out maxId) >= 0;
                        break;
                    case WorkerType.PrizeDraw:
                        result = _mainDbDataLayer.BonusSummaryDrawPrizeSet(workerId ,trans ,out maxId) >= 0;
                        break;
                    case WorkerType.VerificationReward:
                        result = _mainDbDataLayer.BonusSummaryVerificationSet(workerTime.StartTime, workerTime.EndDate ,trans ) >= 0;
                        break;
                    default:
                        break;
                }

                if (option.TimerType == TimerType.Time)
                {
                    if (result && isAutoRun) 
                        result = _mainDbDataLayer.SetBonusSummaryTimer((int) option.WorkerType, workerTime.EndDate ,trans) >= 0;
                }
                else
                {
                    if (result && isAutoRun) 
                        result = _mainDbDataLayer.SetWorkerId((int) option.WorkerType, "PrizeLog" , maxId ,trans) >= 0 ;
                }
                   
                if (result)
                {
                    trans.Commit();
                }
                else
                {
                    await trans.DisposeAsync();
                }
                    
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return JsonConvert.SerializeObject(ex);
            }
        }

        private class JobList : IInputDataList
        {
            public Dictionary<string, string> GetData()
            {
                return _jobList?.Where(i => i.JobName.Contains("BonusSummary"))
                    .ToDictionary(f => f.JobName, f => f.JobName);
            }

            public string GetDefaultValue()
            {
                return null;
            }
        }

        [Job]
        [DisplayName("Run {2} by ManagementPage")]
        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public object Run(
            PerformContext context = null,
            IJobCancellationToken cancellationToken = null,
            [DisplayData("JobName", ConvertType = typeof(JobList))] string jobName = null,
            [DisplayData("BeginDate", placeholderText: "yyyy-MM-dd")] string beginDate = null,
            [DisplayData("EndDate", placeholderText: "yyyy-MM-dd")] string endDate = null,
            [DisplayData("Id", placeholderText: "Id")] string id = null)
        {
            if (!_jobList.Any())
            {
                return null;
            }
            var jobOption = _jobList.First(i => i.JobName == jobName);
            var option = JsonConvert.DeserializeObject<BonusSummaryOption>(JsonConvert.SerializeObject(jobOption.Parameters));

            if (option.TimerType == TimerType.Time)
            {
                var start = DateTime.ParseExact($"{beginDate}", "yyyy-MM-dd", null);
                var end = DateTime.ParseExact($"{endDate}", "yyyy-MM-dd", null);
                var startTime = start.ToString("yyyy-MM-dd");
                var endTime = end.ToString("yyyy-MM-dd");
                jobOption.Parameters.Update("StartTime", startTime);
                jobOption.Parameters.Update("EndDate", endTime);
                return ExecuteAsync($"Execute.{jobName}.({startTime} ~ {endTime})", jobOption, context, cancellationToken).Result;
            }
            else
            {
                jobOption.Parameters.Update("Id", id);
                return ExecuteAsync($"Execute.{jobName}.((Id:{id})", jobOption, context, cancellationToken).Result;
            } 
        }

        private WorkerTime GetWorkerTime( BonusSummaryOption option , out bool isAutoRun)
        {
            DateTime startTime;
            var endTime = DateTime.Now;
            if (!option.StartTime.HasValue || !option.EndDate.HasValue)
            {
                startTime = _reportDbDataLayer.GetBonusSummaryTimer((int)option.WorkerType).StartTime;
                isAutoRun = true;
            }
            else
            {
                startTime = option.StartTime.Value;
                endTime = option.EndDate.Value;
                isAutoRun = false;
            }
            
            startTime = startTime.AddMinutes(-5);

            var workerTime = new WorkerTime()
            {
                StartTime = startTime,
                EndDate = endTime
            };
            
            return workerTime;
        }
        
        private Task<int> GetWorkerId( BonusSummaryOption option , out bool isAutoRun)
        {
            Task<int> id;
            if (!option.Id.HasValue)
            {
                id =  _timerService.GetWorkerId((int)option.WorkerType);
                isAutoRun = true;
            }
            else
            {
                id = Task.FromResult(option.Id.Value);
                isAutoRun = false;
            }

            return id;
        }
    }
}
