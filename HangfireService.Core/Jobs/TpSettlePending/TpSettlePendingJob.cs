using Hangfire;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using Hangfire.Server;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Extensions;
using HangfireService.Core.Helpers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Models.Options;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System.ComponentModel;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("TpSettlePendingJob", queue: "TpSettlePending")]
    public class TpSettlePendingJob : BaseJob, IScheduledJob
    {
        private readonly ReportDBDataLayer _reportDBDataLayer;
        private readonly WagerDBDataLayer _wagerDBDataLayer;

        private readonly TelegramBotHelper _telegramBotHelper;
        private static List<JobOption>? _jobList;

        public TpSettlePendingJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
            _wagerDBDataLayer = provider.GetRequiredService<WagerDBDataLayer>();
            _telegramBotHelper = provider.GetRequiredService<TelegramBotHelper>();
            MyJobManager.GetJobGroups()?.TryGetValue("TpSettlePending", out _jobList);
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");

                var option = JsonConvert.DeserializeObject<TpSettleOption>(JsonConvert.SerializeObject(jobOption.Parameters));
                int subtractDay = option.SubtractDay;
                int categoryId = option.CategoryId;
                string categoryName = option.CategoryName;

                int cnt = 0;
                var begin = DateTime.Today.AddDays(subtractDay).GetUnixTimeByDay();
                var end = DateTime.Today.GetUnixTimeByDay();
                var tpGameTypeData = _reportDBDataLayer.GetGameTypeByCategoryId(categoryId).Result;
                var subMsg = "";

                foreach (var tpGameType in tpGameTypeData)
                {
                    if (tpGameType.TpId == 1 || tpGameType.TpId == 41)
                    {
                        // 排除saba檢查
                        continue;
                    }

                    if (categoryId == 8 && (tpGameType.Type == 47 || tpGameType.Type == 61))
                    {
                        // 排除Poker V8 & MP No Revenue GameType重複檢查 
                        continue;
                    }

                    LogContext.PushProperty("Tp", tpGameType.TpId);
                    LogContext.PushProperty("GameType", tpGameType.TypeName);
                    var bettrans = Enumerable.Empty<TpSettlePendingModel>();
                    var gameIds = new List<long> { 7, 8, 9, 10, 11, 12, 15, 16, 18, 28, 29, 32, 35, 36, 37, 39, 44, 45, 49, 50, 53, 57, 63, 64, 65 };

                    switch (categoryId)
                    {
                        case 1:
                            if (gameIds.Contains(tpGameType.Type))
                            {
                                bettrans = await _reportDBDataLayer.GetTpSettlePendingSportByTpGameType(tpGameType.Type, begin, end);
                            }
                            else
                            {
                                bettrans = await _reportDBDataLayer.GetTpSettlePendingTpUnsettleByTpGameType(tpGameType.TpId, tpGameType.Type, begin, end);
                            }
                            break;
                        case 2:
                            if (tpGameType.Type == 34 || tpGameType.Type == 46)
                            {
                                bettrans = await _wagerDBDataLayer.GetTpSettlePendingSlotByTpGameType(tpGameType.Type, begin, end);
                            }
                            else if (gameIds.Contains(tpGameType.Type))
                            {
                                bettrans = await _reportDBDataLayer.GetTpSettlePendingSlotByTpGameType(tpGameType.Type, begin, end);
                            }
                            else
                            {
                                // 新遊戲結構改用TpUnsettle
                                bettrans = await _reportDBDataLayer.GetTpSettlePendingTpUnsettleByTpGameType(tpGameType.TpId, tpGameType.Type, begin, end);
                            }
                            break;
                        case 3:
                            if (tpGameType.Type == 45 || tpGameType.Type == 48)
                            {
                                bettrans = await _wagerDBDataLayer.GetTpSettlePendingLiveCasinoByTpGameType(tpGameType.Type, begin, end);
                            }
                            else if (gameIds.Contains(tpGameType.Type))
                            {
                                bettrans = await _reportDBDataLayer.GetTpSettlePendingLiveCasinoByTpGameType(tpGameType.Type, begin, end);
                            }
                            else
                            {
                                bettrans = await _reportDBDataLayer.GetTpSettlePendingTpUnsettleByTpGameType(tpGameType.TpId, tpGameType.Type, begin, end);
                            }
                            break;
                        case 4:
                            if (gameIds.Contains(tpGameType.Type))
                            {
                                bettrans = await _reportDBDataLayer.GetTpSettlePendingFishByTpGameType(tpGameType.Type, begin, end);
                            }
                            else
                            {
                                bettrans = await _reportDBDataLayer.GetTpSettlePendingTpUnsettleByTpGameType(tpGameType.TpId, tpGameType.Type, begin, end);
                            }
                            break;
                        case 6:
                            if (gameIds.Contains(tpGameType.Type))
                            {
                                bettrans = await _reportDBDataLayer.GetTpSettlePendingLotteryByTpGameType(tpGameType.Type, begin, end);
                            }
                            else
                            {
                                bettrans = await _reportDBDataLayer.GetTpSettlePendingTpUnsettleByTpGameType(tpGameType.TpId, tpGameType.Type, begin, end);
                            }
                            break;
                        case 8:
                            if (tpGameType.Type == 41 || tpGameType.Type == 42 || tpGameType.Type == 52 || tpGameType.Type == 60)
                            {
                                bettrans = await _wagerDBDataLayer.GetTpSettlePendingPokerByTpGameType(tpGameType.Type, begin, end);
                            }
                            else if (gameIds.Contains(tpGameType.Type))
                            {
                                bettrans = await _reportDBDataLayer.GetTpSettlePendingPokerByTpGameType(tpGameType.Type, begin, end);
                            }
                            break;
                        default:
                            cnt = -1;
                            _logger.LogError($"遊戲: {tpGameType.TypeName}, CategoryName: {categoryName}, CategoryId 尚不支援: {categoryId}");
                            break;
                    }

                    if (bettrans.Any())
                    {
                        cnt += bettrans.Count();

                        var result = bettrans.GroupBy(g => g.TpUserName).Select(s => new
                        {
                            TpUserName = s.Key,
                            TransId = s.Aggregate(string.Empty, (c, n) => c + (string.IsNullOrEmpty(c) ? string.Empty : ", ") + n.TransId)
                        });

                        var msgRow = "未結帳資料:";
                        foreach (var i in result)
                        {
                            msgRow += $"\r\nTpUserName: {i.TpUserName}, TransId: {i.TransId}";
                        }
                        subMsg += $"遊戲: {tpGameType.TypeName}, 未結帳筆數: {bettrans.Count()}\r\n{msgRow}\r\n";
                        //_logger.LogError($"遊戲: {tpGameType.TypeName}, 未結帳筆數: {bettrans.Count()}\r\n{msgRow}");
                    }
                }

                var msg = $"[結帳檢查: {DateTime.Today:yyyy-MM-dd}] 未結帳總筆數: {cnt}\r\nCategoryName: {categoryName}, CategoryId: {categoryId}, 時間範圍: {begin} ~ {end}";
                if (cnt != 0)
                {
                    _logger.LogError($"{msg}\r\n{subMsg}");
                }
                else
                {
                    _logger.LogInformation(msg);
                }

                _telegramBotHelper.SendMessage(msg);
                return msg;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return JsonConvert.SerializeObject(ex);
            }
        }

        private class TpSettlePendingJobList : IInputDataList
        {
            public Dictionary<string, string>? GetData()
            {
                return _jobList?.ToDictionary(f => f.JobName, f => f.JobName);
            }

            public string? GetDefaultValue()
            {
                return null;
            }
        }

        [Job]
        [DisplayName("Run {2} by ManagementPage")]
        public object Run(
            PerformContext? context = null,
            IJobCancellationToken? cancellationToken = null,
            [DisplayData("JobName", ConvertType = typeof(TpSettlePendingJobList))]
            string? jobName = null)
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
