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
using HangfireService.Core.Models.Promotion;
using HangfireService.Core.Promotion;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("BonusSummaryJob", queue: "relay")]
    public class BonusSummaryJob : BaseJob, IScheduledJob
    {
        private readonly MainDBDataLayer _mainDbDataLayer;
        private static List<JobOption> _jobList;
        private readonly IEnumerable<PromotionSummaryBase> _promotion;
        private readonly IBonusSummaryTimerService _timerService;

        public BonusSummaryJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            MyJobManager.GetJobGroups()?.TryGetValue("Relay", out _jobList);
            _mainDbDataLayer = provider.GetRequiredService<MainDBDataLayer>();
            _promotion = provider.GetServices<PromotionSummaryBase>();
            _timerService = provider.GetService<IBonusSummaryTimerService>();
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");
                
                var option = JsonConvert.DeserializeObject<BonusSummaryOption>(jobOption.Parameters);
                
                if (option.WorkerType is WorkerType.Referral or WorkerType.ReferralFreeCredit or WorkerType.VipUpgrade or WorkerType.Other )
                {
                    var promotion = _promotion.Single(a => a.WorkerType == option.WorkerType);
                    
                    if (option.StartTime.HasValue && option.EndTime.HasValue)
                        promotion.ManualOption = new ManualOption() { StartTime = option.StartTime.Value, EndTime = option.EndTime.Value };
                    if (option.Id.HasValue)
                        promotion.ManualOption = new ManualOption() { Id = option.Id.Value };

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
                    workerId = GetWorkerId(option , out isAutoRun);
                }
                    
                await using var conn = _mainDbDataLayer.GetMySqlConnection();
                conn.Open();
                var trans = await conn.BeginTransactionAsync();
                    
                switch (option.WorkerType)
                {
                    case WorkerType.Promotion:
                        result = _mainDbDataLayer.BonusSummaryPromotionSet(workerTime.StartTime, workerTime.EndTime ,trans ) >= 0;
                        break;
                    case WorkerType.Rebate:
                        result = _mainDbDataLayer.BonusSummaryRebateSet(workerId ,trans ,out maxId) >= 0;
                        break;
                    case WorkerType.PrizeDraw:
                        result = _mainDbDataLayer.BonusSummaryDrawPrizeSet(workerId ,trans ,out maxId) >= 0;
                        break;
                    case WorkerType.VerificationReward:
                        result = _mainDbDataLayer.BonusSummaryVerificationSet(workerTime.StartTime, workerTime.EndTime ,trans ) >= 0;
                        break;
                    default:
                        break;
                }

                if (option.TimerType == TimerType.Time)
                {
                    if (result && isAutoRun) 
                        result = _mainDbDataLayer.SetBonusSummaryTimer((int)option.WorkerType, -1 , workerTime.EndTime , "" ,trans) >= 0;
                }
                else
                {
                    if (result && isAutoRun) 
                        result = _mainDbDataLayer.SetBonusSummaryTimer((int)option.WorkerType, maxId , null , "" ,trans) >= 0 ;
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
        public async Task<object> Run(
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
                var isStartTime = DateTime.TryParse(beginDate, out var startTime);
                var isEndTime = DateTime.TryParse(beginDate, out var endTime);
                if (isStartTime && isEndTime)
                {
                    jobOption.Parameters = jobOption.Parameters.Update("StartTime", beginDate);
                    jobOption.Parameters = jobOption.Parameters.Update("EndTime", endDate);
                }
                return await ExecuteAsync($"Execute.{jobName}.({startTime} ~ {endTime})", jobOption, context, cancellationToken);
            }
            else
            {
                jobOption.Parameters = jobOption.Parameters.Update("Id", id);
                return await ExecuteAsync($"Execute.{jobName}.((Id:{id})", jobOption, context, cancellationToken);
            } 
        }

        private WorkerTime GetWorkerTime( BonusSummaryOption option , out bool isAutoRun)
        {
            DateTime startTime;
            var endTime = DateTime.Now;
            if (!option.StartTime.HasValue || !option.EndTime.HasValue)
            {
                startTime = _timerService.GetBonusSummaryTimer((int)option.WorkerType).StartTime;
                isAutoRun = true;
            }
            else
            {
                startTime = option.StartTime.Value;
                endTime = option.EndTime.Value;
                isAutoRun = false;
            }
            
            startTime = startTime.AddMinutes(-5);

            var workerTime = new WorkerTime()
            {
                StartTime = startTime,
                EndTime = endTime
            };
            
            return workerTime;
        }
        
        private int GetWorkerId( BonusSummaryOption option , out bool isAutoRun)
        {
            int id;
            if (!option.Id.HasValue)
            {
                id =  _timerService.GetBonusSummaryTimer((int)option.WorkerType).Id;
                isAutoRun = true;
            }
            else
            {
                id = option.Id.Value;
                isAutoRun = false;
            }

            return id;
        }
    }
}
