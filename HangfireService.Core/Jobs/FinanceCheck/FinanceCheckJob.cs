using System.ComponentModel;
using System.Text;
using Hangfire;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using Hangfire.Server;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Extensions;
using HangfireService.Core.Helpers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Finance;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Models.Options;
using HangfireService.Core.ThirdParty.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("FinanceCheckJob", queue: "check")]
    public class FinanceCheckJob : BaseJob, IScheduledJob
    {
        private readonly ReportDBDataLayer _reportDBDataLayer;

        private readonly Dictionary<int, IFinanceTpHelper> _tpHandler = new();

        private static List<JobOption> _jobList;

        private readonly TelegramBotHelper _telegramBotHelper;

        public FinanceCheckJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
            MyJobManager.GetJobGroups()?.TryGetValue("Check", out _jobList);
            _telegramBotHelper = provider.GetRequiredService<TelegramBotHelper>();
            foreach (var tp in provider.GetServices<IFinanceTpHelper>())
            {
                _tpHandler.Add(tp.TpId, tp);
            }
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");
                var checkOption = JsonConvert.DeserializeObject<FinanceCheckOption>(jobOption.Parameters);
                var tpBetRecordList = _reportDBDataLayer.GetTpBetRecordCheckData(checkOption.BeginTime, checkOption.EndTime)
                                .GroupBy(i => i.TpId).ToDictionary(i => i.Key, i => i.ToList());

                var result = new StringBuilder();
                foreach (var tpBetRecord in tpBetRecordList)
                {
                    if (!_tpHandler.TryGetValue(tpBetRecord.Key, out IFinanceTpHelper tpHelper))
                    {
                        _logger.LogInformation($"TpId:{tpBetRecord.Key} NotImplemented finance check");
                        continue;
                    }

                    var bettransList = await tpHelper.GetBettransCheckData(checkOption);
                    var tpDailyList = await tpHelper.GetTpDailySummaryCheckData(checkOption);
                    var tpBetRecordData = (tpHelper.BindCommonCurrency) ? tpHelper.ProcessTpBetRecordSubCurrencyToMainCurrency(tpBetRecord.Value) : tpBetRecord.Value;
                    var checkResult = tpHelper.CheckData(checkOption, tpBetRecordData, bettransList.ToList(), tpDailyList.ToList());
                    if (checkResult.Any())
                    {
                        var msg = GetMessage(tpBetRecord.Key, checkOption, checkResult);
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
            PerformContext? context = null,
            IJobCancellationToken? cancellationToken = null,
            [DisplayData("JobName", ConvertType = typeof(JobList))] string jobName = null,
            [DisplayData("CheckDate", ConvertType = typeof(CheckDateList))] string CheckDate = null)
        {
            if (!_jobList.Any())
            {
                return null;
            }
            var jobOption = _jobList.First(i => i.JobName == jobName);
            var date = DateTime.ParseExact($"{CheckDate}", "yyyy-MM-dd", null);
            var BeginTime = date.ToString("yyyy-MM-dd");
            var EndTime = date.AddDays(1).ToString("yyyy-MM-dd");
            jobOption.Parameters = jobOption.Parameters.Update("BeginTime", BeginTime);
            jobOption.Parameters = jobOption.Parameters.Update("EndTime", EndTime);

            return ExecuteAsync($"Execute.{jobName}.({BeginTime}~{EndTime})", jobOption, context, cancellationToken).Result;
        }

        private string GetMessage(int TpId, FinanceCheckOption checkOption, IEnumerable<FinanceCheckResult> checkResults)
        {
            var result = new StringBuilder();
            var checkFilter = checkResults.Where(i => i.ErrorList.Any());
            string specialMsg = "";
            if (TpId == 30)
            {
                specialMsg = "(GameType: 50)";
            }
            else if (TpId == 6)
            {
                specialMsg = "(GameType: 88~91)";
            }

            if (!checkFilter.Any())
            {
                result.AppendLine($"[{checkOption.BeginTime:yyyy-MM-dd}] TpId:{TpId} 帳務檢查正常 {specialMsg}");
                return result.ToString();
            }


            result.AppendLine($"[{checkOption.BeginTime:yyyy-MM-dd}] TpId:{TpId} 帳務檢查異常  {specialMsg}");
            var group = checkFilter.GroupBy(i => i.CurrencyId);
            foreach (var check in group)
            {
                result.AppendLine($"\u3000CurrencyId:{check.Key}");
                foreach (var item in check.OrderBy(i => i.FinanceCheckType))
                {
                    result.AppendLine($"\u3000\u3000{item.FinanceCheckType}");
                    foreach (var error in item.ErrorList)
                    {
                        result.AppendLine($"\u3000\u3000\u3000 - {error}");
                    }
                }
            }
            return result.ToString();
        }
        private class JobList : IInputDataList
        {
            public Dictionary<string, string>? GetData()
            {
                return _jobList?.Where(i => i.JobName.Contains("Finance.TpBetRecord.Check"))
                    .ToDictionary(f => f.JobName, f => f.JobName);
            }
            public string? GetDefaultValue()
            {
                return null;
            }
        }

        private class CheckDateList : IInputDataList
        {
            public Dictionary<string, string>? GetData()
            {
                var yesterday = DateTime.Today.AddDays(-1);
                var begin = yesterday.AddDays(-12);
                var result = new List<string>();
                while (begin <= yesterday)
                {
                    result.Add(begin.ToString("yyyy-MM-dd"));
                    begin = begin.AddDays(1);
                }
                return result.OrderByDescending(i => i).ToDictionary(f => f, f => f);
            }

            public string? GetDefaultValue()
            {
                return null;
            }
        }
    }
}
