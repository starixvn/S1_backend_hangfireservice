using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Newtonsoft.Json;
using System.ComponentModel;
using Hangfire.Server;
using Hangfire;
using Hangfire.Dashboard.Management.Metadata;
using HangfireService.Core.Models.Options;
using Hangfire.Dashboard.Management.Support;
using HangfireService.Core.Extensions;
using HangfireService.Core.Helpers;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("RelayJob", queue: "relay")]
    public class RelayJob : BaseJob, IScheduledJob
    {
        private readonly MainDBDataLayer _mainDBDataLayer;
        private readonly ReportDBDataLayer _reportDBDataLayer;
        private static List<JobOption> _jobList;
        private readonly TelegramBotHelper _telegramBotHelper;

        public RelayJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            MyJobManager.GetJobGroups()?.TryGetValue("Relay", out _jobList);
            _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
            _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
            _telegramBotHelper = provider.GetRequiredService<TelegramBotHelper>();
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");
                var option = JsonConvert.DeserializeObject<RelayOption>(JsonConvert.SerializeObject(jobOption.Parameters));
                var result = 0;

                if (option.ExecuteType == ExecuteType.Days)
                {
                    //逐日進行
                    var date = option.StartTime;
                    while (date <= option.EndTime)
                    {
                        _logger.LogInformation($"{jobName} || Excute Summary Date:{date::yyyy-MM-dd}");
                        result += await ExcuteRelay(option, date, date.AddDays(1));
                        date = date.AddDays(1);
                    }
                }
                else
                {
                    result += await ExcuteRelay(option, option.StartTime, option.EndTime);
                }

                return $"{jobName}.({option.StartTime:yyyy-MM-dd HH:mm} ~ {option.EndTime:yyyy-MM-dd HH:mm}) success count: {result}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                BackgroundJob.Schedule(
                    () => ExecuteAsync(jobName, jobOption, null, null),
                    TimeSpan.FromMinutes(5));
                _telegramBotHelper.SendMessage($"{jobName} 執行發生錯誤，五分鐘後重試。");
                return JsonConvert.SerializeObject(ex);
            }
        }


        private async Task<int> ExcuteRelay(RelayOption option, DateTime StartTime, DateTime EndTime)
        {
            var result = 0;
            var StartIndex = 0;
            var PageSize = option.LiminNum;

            var parameters = new
            {
                StartTime,
                EndTime,
                StartIndex,
                PageSize,
            };
            var relayData = _reportDBDataLayer.GetRelayData(option.GetSP, parameters, option.TimeoutSeconds);       
            var forContinue = relayData.Count() == PageSize;

            using var conn = _mainDBDataLayer.GetMySqlConnection();
            conn.Open();
            var trans = conn.BeginTransaction();
            if (relayData.Any())
            {
                //清除舊資料
                _mainDBDataLayer.DeleteRelayData(option.DeleteSP, parameters, trans);
                
                //寫入資料
                result += _mainDBDataLayer.InsertRelayData(option.InsertSP, relayData, trans);
            }

            //處理分頁資料
            while (forContinue)
            {
                StartIndex += PageSize;
                relayData = _reportDBDataLayer.GetRelayData(option.GetSP, new
                {
                    StartTime,
                    EndTime,
                    StartIndex,
                    PageSize,
                }, option.TimeoutSeconds);
                forContinue = relayData.Count() == PageSize;
                
                //寫入分頁其餘資料
                result += _mainDBDataLayer.InsertRelayData(option.InsertSP, relayData, trans);                                      
            }

            if (result > 0)
            {
                trans.Commit();
            }
            else
            {
                trans.Dispose();
            }

            return result;
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
            [DisplayData("LiminNum", defaultValue:"2000")]string liminNum = "2000")
        {
            if (!_jobList.Any())
            {
                return null;
            }
            var jobOption = _jobList.First(i => i.JobName == jobName);
            var start = DateTime.ParseExact($"{beginDate}", "yyyy-MM-dd", null);
            var end = DateTime.ParseExact($"{endDate}", "yyyy-MM-dd", null);


            //TpBetRecord only 14day
            if (jobName.Contains("TpBetRecordRelay") )
            {
                var last14 = DateTime.UtcNow.Date.AddDays(-14);
                if (start < last14 || end < last14)
                {
                    throw new InvalidOperationException("TpBetRecordRelay date more then 14 day.");
                }
            }

            var StartTime = start.ToString("yyyy-MM-dd");
            var EndTime = end.ToString("yyyy-MM-dd");
            jobOption.Parameters.Update("StartTime", StartTime);
            jobOption.Parameters.Update("EndDate", EndTime);
            jobOption.Parameters.Update("LiminNum", liminNum);
            return ExecuteAsync($"Execute.{jobName}.({StartTime} ~ {EndTime})", jobOption, context, cancellationToken).Result;
        }

        private class JobList : IInputDataList
        {
            public Dictionary<string, string> GetData()
            {
                return _jobList?.Where(i => i.JobName.Contains("Relay-Days") || i.JobName.Contains("Summary-Days"))
                    .ToDictionary(f => f.JobName, f => f.JobName);
            }
            public string GetDefaultValue()
            {
                return null;
            }
        }
    }
}
