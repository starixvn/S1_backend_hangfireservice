using Hangfire;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using Hangfire.Server;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Helpers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Models.Options;
using HangfireService.Core.ThirdParty.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System.ComponentModel;
using System.Linq;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("FinanceCheckTransIdHandlingJob", queue: "check")]
    internal class FinanceCheckTransIdHandlingJob : BaseJob, IScheduledJob
    {
        private readonly ReportDBDataLayer _reportDBDataLayer;
        private readonly MainDBDataLayer _mainDBDataLayer;

        private readonly Dictionary<int, IFinanceTpHelper> _tpHandler = new();

        private static List<JobOption> _jobList;

        private readonly TelegramBotHelper _telegramBotHelper;

        public FinanceCheckTransIdHandlingJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            MyJobManager.GetJobGroups()?.TryGetValue("Check", out _jobList);

            _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
            _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
            _telegramBotHelper = provider.GetRequiredService<TelegramBotHelper>();
            foreach (var tp in provider.GetServices<IFinanceTpHelper>())
            {
                // 詳細檢查TransId
                if (tp.CheckTransId)
                {
                    _tpHandler.Add(tp.TpId, tp);
                }
            }
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");

                // 取得FinanceCheckTransIdJob檢查異常資料
                var handlingList = _reportDBDataLayer.GetFinanceCheckTransIdHandling();
                if (!handlingList.Any())
                {
                    _logger.LogInformation("No Data.");
                    return true;
                }

                var timeOptionId = TimeOption.TransId;
                var tpIds = handlingList.Select(i => i.TpId).ToList();
                var bettransOfTransIds = handlingList.Where(i => i.FinanceCheckTransIdType == (int)FinanceCheckTransIdType.結算與交易單號檢查).Select(x => x.TransId).ToList();
                var tpBetRecordOfTransIds = handlingList.Where(i => i.FinanceCheckTransIdType == (int)FinanceCheckTransIdType.交易與結算單號檢查).Select(x => x.TransId).ToList();
                var checkTransIdExist = handlingList.Where(i => i.FinanceCheckTransIdType == (int)FinanceCheckTransIdType.結算與交易單號檢查 || i.FinanceCheckTransIdType == (int)FinanceCheckTransIdType.交易與結算單號檢查);
                var checkAmtMatch = handlingList.Where(i => i.FinanceCheckTransIdType == (int)FinanceCheckTransIdType.結算與交易金額檢查).GroupBy(x => x.TpId);

                // 依FinanceCheckTransIdType處理情況
                foreach (var item in checkTransIdExist)
                {
                    var type = item.FinanceCheckTransIdType;
                    var tpId = item.TpId;
                    var tid = new List<string> { item.TransId };
                    var checkOption = new FinanceCheckTransIdOption
                    {
                        Mode = ConditionOption.TransId,
                        TransIds = JsonConvert.SerializeObject(tid)
                    };

                    switch (type)
                    {
                        case (int)FinanceCheckTransIdType.結算與交易單號檢查:
                            // 檢查單號是否有存在bettrans
                            if (bettransOfTransIds.Any() && bettransOfTransIds.Contains(item.TransId))
                            {
                                // 取出bettrans單號資料
                                var tpHelper = _tpHandler[tpId];
                                var bettransList = await tpHelper.GetBettransCheckTransIdData(checkOption);
                                if (bettransList.Any())
                                {
                                    // 存在刪除
                                    DeleteFinanceCheckTransId(item.SQ);
                                }
                            }
                            // 不存在等待通知
                            break;
                        case (int)FinanceCheckTransIdType.交易與結算單號檢查:
                            // 檢查單號是否有存在tpbetrecord
                            if (tpBetRecordOfTransIds.Any() && tpBetRecordOfTransIds.Contains(item.TransId))
                            {
                                // 取出tpbetrecord單號資料
                                var tpBetRecord = _reportDBDataLayer.GetTpBetRecordCheckTransIdData(checkOption.BeginTime, checkOption.EndTime, tpId, timeOptionId, checkOption.TransIds);
                                if (tpBetRecord.Any())
                                {
                                    // 存在刪除
                                    DeleteFinanceCheckTransId(item.SQ);
                                }
                            }
                            // 不存在等待通知
                            break;
                        default:
                            break;
                    }
                }

                foreach (var group in checkAmtMatch)
                {
                    var tpId = group.Key;
                    var tpHelper = _tpHandler[tpId];

                    var AmountOfTransIds = group.Select(i => i.TransId).ToList();
                    if (!AmountOfTransIds.Any())
                    {
                        _logger.LogWarning("[結算與交易金額檢查] - 無資料處理");
                        continue;
                    }

                    var checkOption = new FinanceCheckTransIdOption
                    {
                        Mode = ConditionOption.TransId,
                        TransIds = JsonConvert.SerializeObject(AmountOfTransIds)
                    };

                    // 取出tpbetrecord單號資料
                    var tpBetRecord = _reportDBDataLayer.GetTpBetRecordCheckTransIdData(checkOption.BeginTime, checkOption.EndTime, tpId, timeOptionId, checkOption.TransIds);
                    if (tpBetRecord == null || !tpBetRecord.Any())
                    {
                        _logger.LogError("[結算與交易金額檢查] - tpbetrecord金額空資料");
                        continue;
                    }

                    // 取出bettrans單號資料
                    var bettransList = await tpHelper.GetBettransCheckTransIdData(checkOption);
                    if (bettransList == null || !bettransList.Any())
                    {
                        _logger.LogError("[結算與交易金額檢查] - bettrans金額空資料");
                        continue;
                    }

                    // 金額檢查(只回傳不符合的資料)
                    var tpResult = bettransList.Select(bettrans
                        => bettrans.CheckTransIdData(FinanceCheckTransIdType.結算與交易金額檢查, tpBetRecord.FirstOrDefault(tpBetRecord => tpBetRecord.TransId == bettrans.TransId)));
                    var tpData = tpResult.Where(item => item != null).ToList();
                    foreach (var d in AmountOfTransIds)
                    {
                        if (!tpData.Any(i => i.TransId == d))
                        {
                            // 符合刪除
                            DeleteFinanceCheckTransId(group.First(i => i.TransId == d).SQ);
                        }
                    }
                }

                // 重新取異常資料TG通知
                handlingList = _reportDBDataLayer.GetFinanceCheckTransIdHandling();
                if (handlingList.Any())
                {
                    var msg = handlingList.GroupBy(item => item.FinanceCheckTransIdType).Select(g => new
                    {
                        Type = typeof(FinanceCheckTransIdType).GetEnumName(g.Key),
                        ErrorCount = g.Count(),
                        TransIds = g.Select(x => x.TransId).ToList(),
                    });
                    _logger.LogError(JsonConvert.SerializeObject(msg));

                    var tg = msg.Select(g => new
                    {
                        Type = g.Type,
                        ErrorCount = g.ErrorCount
                    });
                    //_telegramBotHelper.SendMessage(JsonConvert.SerializeObject(tg));
                    return true;
                }
                _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} finish......");
                return true;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return JsonConvert.SerializeObject(ex);
            }
        }

        [Job]
        [DisplayName("Run {2} by ManagementPage")]
        public object Run(
            PerformContext context = null,
            IJobCancellationToken cancellationToken = null,
            [DisplayData("JobName", ConvertType = typeof(JobList))] string jobName = null
        )
        {
            if (!_jobList.Any())
            {
                return null;
            }
            var jobOption = _jobList.First(i => i.JobName == jobName);
            return ExecuteAsync($"Execute.{jobName}.", jobOption, context, cancellationToken).Result;
        }

        private bool DeleteFinanceCheckTransId(long SQ)
        {
            var exec = _mainDBDataLayer.DeleteFinanceCheckTransId(SQ);
            return exec > 0;
        }

        private class JobList : IInputDataList
        {
            public Dictionary<string, string> GetData()
            {
                return _jobList?.Where(i => i.JobName.Contains("Finance.TpBetRecord.TransId.Handling.Check"))
                    .ToDictionary(f => f.JobName, f => f.JobName);
            }
            public string GetDefaultValue()
            {
                return null;
            }
        }
    }
}
