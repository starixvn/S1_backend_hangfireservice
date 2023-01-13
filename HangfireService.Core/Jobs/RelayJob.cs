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
using MySql.Data.MySqlClient;

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
                var option = JsonConvert.DeserializeObject<RelayOption>(jobOption.Parameters);
                var result = 0;

                if (option.ExecuteType == ExecuteType.Days)
                {
                    //先逐日進行
                    var date = option.StartTime;
                    while (date <= option.EndTime)
                    {
                        _logger.LogInformation($"{jobName} || Excute Summary Date:{date:yyyy-MM-dd}");
                        //再用每小時執行
                        var hours = date;//每日00:00
                        var end = date.AddDays(1);//每日24:00
                        while (hours < end)
                        {
                            var nextHours = hours.AddHours(1);
                            _logger.LogInformation($"{jobName} || Excute Summary Hour:{hours:yyyy-MM-dd HH:mm} ~ {nextHours:HH:mm}");
                            result += await ExcuteRelay(option, hours, nextHours);
                            hours = nextHours;
                        }
                       
                        date = date.AddDays(1);
                    }
                }
                else
                {
                    result += await ExcuteRelay(option, option.StartTime, option.EndTime);
                }

                //洗舊資料用
                if (jobName.Contains("TpBetRecordSummary-Days") && option.StartTime == option.EndTime 
                    && option.StartTime >= new DateTime(2022,01,01))
                {
                    var lastDay = option.StartTime.ToString("yyyy-MM-dd");
                    var nextDay = option.StartTime.AddDays(-1).ToString("yyyy-MM-dd");
                    jobOption.Parameters = jobOption.Parameters.Update("StartTime", nextDay);
                    jobOption.Parameters = jobOption.Parameters.Update("EndTime", value: nextDay);
                    var newJobName = jobName.Replace(lastDay, nextDay);
                    BackgroundJob.Schedule(
                      () => ExecuteAsync(newJobName, jobOption, null, null),
                       TimeSpan.FromSeconds(5));
                }

                return option.ExecuteType == ExecuteType.Days ? 
                    $"{jobName}.({option.StartTime:yyyy-MM-dd} ~ {option.EndTime:yyyy-MM-dd}) success count: {result}":
                    $"{jobName}.({option.StartTime:yyyy-MM-dd HH:mm} ~ {option.EndTime:yyyy-MM-dd HH:mm}) success count: {result}"; ;
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
            var parameters = new
            {
                StartTime,
                EndTime,
                SummaryTypeList = option.SummaryType
            };
            var relayData = _reportDBDataLayer.GetRelayData(option.GetSP, parameters, option.TimeoutSeconds);       

            using var conn = _mainDBDataLayer.GetMySqlConnection();
            conn.Open();
            var trans = conn.BeginTransaction();
            if (relayData.Any())
            {
                //清除舊資料
                _mainDBDataLayer.DeleteRelayData(option.DeleteSP, parameters, trans);
                try
                {
                    //寫入資料
                    result += _mainDBDataLayer.InsertRelayData(option.InsertSP, relayData, trans);
                }
                catch (MySqlException mySqlEx)
                {
                    //Saba二次結算會造成Duplicatekey，先記錄就好不中斷relay流程，讓每日排程更新資料
                    _logger.LogWarning(mySqlEx, mySqlEx.Message);
                    if (mySqlEx.Number != 1062)
                    {
                        throw;
                    }
                }
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
            [DisplayData("EndDate", placeholderText: "yyyy-MM-dd")] string endDate = null)
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
            jobOption.Parameters = jobOption.Parameters.Update("StartTime", StartTime);
            jobOption.Parameters = jobOption.Parameters.Update("EndTime", EndTime);
            var result = ExecuteAsync($"Execute.{jobName}.({StartTime} ~ {EndTime})", jobOption, context, cancellationToken).Result;
            
          
            return result;
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
