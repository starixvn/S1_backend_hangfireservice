using System.ComponentModel;

using Hangfire;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using Hangfire.Server;

using HangfireService.Core.DataLayers;
using HangfireService.Core.Helpers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Jobs;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Serilog.Context;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("TpWagerTimeCheckJob", queue: "check")]
    public class TpWagerTimeCheckJob : BaseJob, IScheduledJob
    {
        private readonly ReportDBDataLayer _reportDBDataLayer;
        private readonly WagerDBDataLayer _wagerDBDataLayer;

        private readonly TelegramBotHelper _telegramBotHelper;
        private static List<JobOption> _jobList;

        public TpWagerTimeCheckJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
            _wagerDBDataLayer = provider.GetRequiredService<WagerDBDataLayer>();
            _telegramBotHelper = provider.GetRequiredService<TelegramBotHelper>();
            MyJobManager.GetJobGroups()?.TryGetValue("Check", out _jobList);
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");

                // get tp list
                var tpListData = _reportDBDataLayer.GetAllTpList();

                // get timer list
                var tpWagerTimerData = await _wagerDBDataLayer.GetTpWagerTimerList();
                var tpWagerTimerReference = await _wagerDBDataLayer.GetTpWagerTimerReferenceList();

                // data merge
                var allData = tpWagerTimerData.Concat(tpWagerTimerReference);

                var now = DateTime.Parse(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:00"));
                var subMsg = "";
                List<TpWagerTimerModel> checkList = new();

                foreach (var data in allData)
                {
                    var tpName = tpListData.First(i => i.TpId == data.TpId).TpName;
                    var delay = now.Subtract(data.StartTime.AddSeconds(1));
                    int delayHour = (int)delay.TotalHours;
                    int delayDay = (int)delay.TotalDays;

                    switch (data.Type)
                    {
                        case 1:
                        case 14:
                        case 52:
                        case 77:
                        case 81:
                            // 正常抄單
                            if (data.TpId == 22)
                            {
                                if (delayHour > 26)
                                {
                                    //每天UTC+0 06:00:00，才會取得前一天的資料，故調整檢查為26小時
                                    checkList.Add(data);
                                }
                            }
                            else if (delayHour >= 1 || delayHour <= -1)
                            {
                                checkList.Add(data);
                            }
                            break;
                        case 2:
                            // 重作抄單
                            var hour4 = new List<int> { 6, 21, 55, 57 };
                            var hour8 = new List<int> { 36 };
                            var day2 = new List<int> { 31 };
                            var day4 = new List<int> { 28 };

                            if (hour4.Contains(data.TpId))
                            {
                                // 落後4小時
                                if (delayHour >= 4)
                                {
                                    checkList.Add(data);
                                }
                            }
                            else if (hour8.Contains(data.TpId))
                            {
                                // 落後8小時
                                if (delayHour >= 8)
                                {
                                    checkList.Add(data);
                                }
                            }
                            else if (day2.Contains(data.TpId))
                            {
                                // 落後2天
                                if (delayDay >= 2)
                                {
                                    checkList.Add(data);
                                }
                            }
                            else if (day4.Contains(data.TpId))
                            {
                                // 落後4天
                                if (delayDay >= 4)
                                {
                                    checkList.Add(data);
                                }
                            }
                            else if (delayHour >= 2)
                            {
                                // 落後2小時
                                checkList.Add(data);
                            }
                            else if (delayDay <= -1)
                            {
                                // 超前1天
                                checkList.Add(data);
                            }
                            break;
                        case 3:
                            // 其他
                            if (data.TpId == 31)
                            {
                                if (delayHour > 7)
                                {
                                    checkList.Add(data);
                                }
                            }
                            else if (delayHour >= 1 || delayHour <= -1)
                            {
                                checkList.Add(data);
                            }
                            break;
                        case 4:
                            // 廠商報表
                            if (delayDay >= 2 || delayDay <= -1)
                            {
                                checkList.Add(data);
                            }
                            break;
                        case 6:
                        case 7:
                            if (data.TpId == 31)
                            {
                                // 落後2天
                                if (delayDay >= 2)
                                {
                                    checkList.Add(data);
                                }
                            }
                            break;
                        default:
                            _logger.LogWarning($"Game: {tpName}, Type currently unsupported: {data.Type}");
                            break;
                    }
                }
                if (checkList.Any())
                {
                    var result = checkList.GroupBy(g => g.TpId).Select(s => new
                    {
                        TpId = s.Key,
                        TpName = tpListData.First(i => i.TpId == s.Key).TpName,
                        Detail = JsonConvert.SerializeObject(s),
                    });

                    var msgRow = "Exceptional data: ";
                    foreach (var i in result)
                    {
                        msgRow += $"\r\ngame: {i.TpName}, information: {i.Detail}";
                    }
                    subMsg += $"{msgRow}\r\nPlease prioritize checking Type 1 data. If the game is under maintenance, please ignore it. For detailed information, refer to the Hangfire Log.";
                }

                var msg = $"[Order duplication check: {now:yyyy-MM-dd HH:mm:ss}] check the count of abnormal records: {checkList.Count} \r\n{subMsg}";
                if (checkList.Any())
                {
                    _logger.LogError(msg);
                    _telegramBotHelper.SendMessage(msg);
                }
                else
                {
                    _logger.LogInformation(msg);
                }

                return msg;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return JsonConvert.SerializeObject(ex);
            }
        }

        private class TpWagerTimeCheckJobList : IInputDataList
        {
            public Dictionary<string, string> GetData()
            {
                return _jobList?.Where(i => i.JobName.Contains("TpWager.Time.Check"))
                    .ToDictionary(f => f.JobName, f => f.JobName);
            }

            public string GetDefaultValue()
            {
                return null;
            }
        }

        [Job]
        [DisplayName("Run {2} by ManagementPage")]
        public object Run(
            PerformContext context = null,
            IJobCancellationToken cancellationToken = null,
            [DisplayData("JobName", ConvertType = typeof(TpWagerTimeCheckJobList))]
            string jobName = null)
        {
            if (!_jobList.Any())
            {
                return null;
            }

            var jobOption = _jobList.First(i => i.JobName == jobName);

            return ExecuteAsync($"Execute.{jobName}", jobOption, context, cancellationToken).Result;
        }
    }
}
