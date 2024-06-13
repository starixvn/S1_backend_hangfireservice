using System.ComponentModel;

using Hangfire;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using Hangfire.Server;

using HangfireService.Core.DataLayers;
using HangfireService.Core.Extensions;
using HangfireService.Core.Helpers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Service;
using Microsoft.Extensions.Logging;

using MySql.Data.MySqlClient;

using Newtonsoft.Json;

using Serilog.Context;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("WinLossSummaryJob", queue: "relay")]
    public class WinLossSummaryJob : BaseJob, IScheduledJob
    {
        private readonly ReportCenterDataLayer _reportCenterDBDataLayer;
        private readonly ReportBackupDBDataLayer _reportBackupDBDataLayer;
        private static List<JobOption> _jobList;
        private readonly TelegramBotHelper _telegramBotHelper;
        private readonly WinLossSummaryService _winLossSummaryService;

        public WinLossSummaryJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            MyJobManager.GetJobGroups()?.TryGetValue("Relay", out _jobList);
            _reportCenterDBDataLayer = provider.GetRequiredService<ReportCenterDataLayer>();
            _reportBackupDBDataLayer = provider.GetRequiredService<ReportBackupDBDataLayer>();
            _telegramBotHelper = provider.GetRequiredService<TelegramBotHelper>();
            _winLossSummaryService = provider.GetRequiredService<WinLossSummaryService>();
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
                //TpBetRecordSummary-Days：StartTime=EndTime，會從StartTime By Days 往前洗，洗到2022/01/01以前的資料
                //TpBetRecordSummary-DaysBA：StartTime=EndTime，會從StartTime By Days 往前洗，洗到2023/04/01以前的資料
                if ((jobName.Contains("WinLossSummary-Days")
                    && option.StartTime == option.EndTime && option.StartTime >= new DateTime(2022, 01, 01)))
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

                var resultStr = option.ExecuteType == ExecuteType.Days ?
                    $"{jobName}.({option.StartTime:yyyy-MM-dd} ~ {option.EndTime:yyyy-MM-dd}) success count: {result}" :
                    $"{jobName}.({option.StartTime:yyyy-MM-dd HH:mm} ~ {option.EndTime:yyyy-MM-dd HH:mm}) success count: {result}"; ;

                _logger.LogInformation(resultStr);

                return resultStr;
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
                EndTime
            };

            var relayData = await _reportBackupDBDataLayer.GetWinLossSourceDataAsync(option.GetSP, parameters, option.TimeoutSeconds);

            if (relayData.Any())
            {
                var dataResult = await _winLossSummaryService.ProcessDataAsync(relayData, StartTime, EndTime);

                await using var conn = _reportCenterDBDataLayer.GetMySqlConnection();
                conn.Open();
                var trans = await conn.BeginTransactionAsync();

                //清除舊資料
                _reportCenterDBDataLayer.DeleteWinLossData(option.DeleteSP, parameters, trans);
                try
                {
                    //寫入資料
                    result += _reportCenterDBDataLayer.InsertWinLossData(option.InsertSP, dataResult, trans);
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

                //二次結算有可能會造成Get跟Insert數量不一樣，所以result > 0 就可以commit
                if (result > 0)
                {
                    trans.Commit();
                    await conn.CloseAsync();
                    await conn.DisposeAsync();
                }
                else
                {
                    await trans.DisposeAsync();
                    await conn.CloseAsync();
                    await conn.DisposeAsync();
                }
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
            if (jobName.Contains("TpBetRecordRelay"))
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
                return _jobList?.Where(i => i.JobName.Contains("WinLossSummary-Days"))
                    .ToDictionary(f => f.JobName, f => f.JobName);
            }
            public string GetDefaultValue()
            {
                return null;
            }
        }
    }
}
