using Hangfire;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using Hangfire.Server;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Extensions;
using HangfireService.Core.Helpers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Finance;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Models.Options;
using HangfireService.Core.ThirdParty.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System.ComponentModel;
using System.Text;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("FinanceCheckTransIdJob", queue: "check")]
    internal class FinanceCheckTransIdJob : BaseJob, IScheduledJob
    {
        private readonly ReportDBDataLayer _reportDBDataLayer;

        private readonly Dictionary<int, IFinanceTpHelper> _tpHandler = new();

        private static List<JobOption> _jobList;

        private readonly TelegramBotHelper _telegramBotHelper;

        public FinanceCheckTransIdJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            MyJobManager.GetJobGroups()?.TryGetValue("Check", out _jobList);

            _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
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

                var checkOption = JsonConvert.DeserializeObject<FinanceCheckTransIdOption>(jobOption.Parameters);
                var result = new StringBuilder();

                foreach (var item in _tpHandler.OrderBy(i => i.Key))
                {
                    var tpId = item.Key;
                    var tpHelper = item.Value;
                    var timeOptionId = tpHelper.TimeOptionId;

                    var tpBetRecordList = _reportDBDataLayer.GetTpBetRecordCheckTransIdData(checkOption.BeginTime, checkOption.EndTime, tpId, timeOptionId);
                    var bettransList = await tpHelper.GetBettransCheckTransIdData(checkOption);
                    var checkResult = tpHelper.CheckTransIdData(checkOption, tpBetRecordList.ToList(), bettransList.ToList());
                    if (checkResult.Any())
                    {
                        var msg = GetMessage(tpId, checkOption, checkResult);
                        _logger.LogError(msg);
                        _telegramBotHelper.SendMessage(msg);
                        result.AppendLine(msg);
                    }
                }

                return result.ToString();
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
            [DisplayData("JobName", ConvertType = typeof(JobList))] string jobName = null,
            [DisplayData("CheckTime", placeholderText: "yyyy-MM-dd HH:00:00")] string CheckTime = null)
        {
            if (!_jobList.Any())
            {
                return null;
            }
            var jobOption = _jobList.First(i => i.JobName == jobName);
            var date = DateTime.ParseExact($"{CheckTime}", "yyyy-MM-dd HH:00:00", null);
            var BeginTime = date.ToString("yyyy-MM-dd HH:00:00");
            var EndTime = date.AddHours(1).ToString("yyyy-MM-dd HH:00:00");
            jobOption.Parameters = jobOption.Parameters.Update("BeginTime", BeginTime);
            jobOption.Parameters = jobOption.Parameters.Update("EndTime", EndTime);

            return ExecuteAsync($"Execute.{jobName}.({BeginTime}~{EndTime})", jobOption, context, cancellationToken).Result;
        }

        private string GetMessage(int TpId, FinanceCheckTransIdOption checkOption, IEnumerable<FinanceCheckTransIdResult> checkResults)
        {
            var result = new StringBuilder();
            var checkFilter = checkResults.Where(i => i.ErrorList.Any());

            if (!checkFilter.Any())
            {
                result.AppendLine($"[{checkOption.BeginTime:yyyy-MM-dd HH:00:00}] TpId:{TpId} 帳務單號檢查正常");
                return result.ToString();
            }

            result.AppendLine($"[{checkOption.BeginTime:yyyy-MM-dd HH:00:00}] TpId:{TpId} 帳務單號檢查異常");
            foreach (var check in checkResults)
            {
                result.AppendLine($"\u3000\u3000{check.FinanceCheckTransIdType}");

                if (check.FinanceCheckTransIdType == FinanceCheckTransIdType.結算與交易資料檢查)
                {
                    foreach (var error in check.ErrorList)
                    {
                        result.AppendLine($"\u3000\u3000\u3000 - {error}");
                    }
                }
                else if (check.FinanceCheckTransIdType == FinanceCheckTransIdType.結算與交易金額檢查)
                {
                    _logger.LogError($"{check.FinanceCheckTransIdType}-異常資料\r\n{check.ErrorData}");
                    result.AppendLine($"\u3000\u3000\u3000 異常筆數 - {check.Count}");
                }
                else
                {
                    _logger.LogError($"{check.FinanceCheckTransIdType}-異常資料\r\n{JsonConvert.SerializeObject(check.ErrorList)}");
                    result.AppendLine($"\u3000\u3000\u3000 異常筆數 - {check.Count}");
                }
            }
            return result.ToString();
        }
        private class JobList : IInputDataList
        {
            public Dictionary<string, string> GetData()
            {
                return _jobList?.Where(i => i.JobName.Contains("Finance.TpBetRecord.TransId.Check"))
                    .ToDictionary(f => f.JobName, f => f.JobName);
            }
            public string GetDefaultValue()
            {
                return null;
            }
        }
    }
}
