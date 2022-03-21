using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Newtonsoft.Json;
using HangfireService.Core.ThirdParty.Interfaces;
using System.ComponentModel;
using Hangfire.Server;
using Hangfire;
using HangfireService.Core.Models.Options;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using HangfireService.Core.Helpers;
using HangfireService.Core.Models.Enums;
using System.Text;
using HangfireService.Core.Models.Finance;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("FinanceCheckJob", queue: "Finance")]
    public class FinanceCheckJob : BaseJob, IScheduledJob
    {
        private readonly ReportDBDataLayer _reportDBDataLayer;

        private readonly Dictionary<int, IFinanceTpHelper> _tpHandler = new();

        private static List<JobOption>? _jobList;

        private readonly TelegramBotHelper _telegramBotHelper;

        public FinanceCheckJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
            MyJobManager.GetJobGroups()?.TryGetValue("Finance", out _jobList);
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
                var checkOption = JsonConvert.DeserializeObject<FinanceCheckOption>(JsonConvert.SerializeObject(jobOption.Parameters));
                var checkList = await _reportDBDataLayer.GetTpBetRecordCheckData(checkOption.BeginTime, checkOption.EndTime);

                var result = new StringBuilder();
                foreach (var check in checkList.GroupBy(i => i.TpId))
                {
                    if (!_tpHandler.TryGetValue(check.Key, out IFinanceTpHelper tpHelper))
                    {
                        _logger.LogInformation($"TpId:{check.Key} NotImplemented finance check");
                        continue;
                    }

                    if (tpHelper.CanCheckBettransData)
                    {
                        var bettransResult = await tpHelper.CheckBettransData(checkOption, check);
                        if (bettransResult.Any())
                        {
                            var msg = GetMessage(FinanceCheckType.BettransData, checkOption, bettransResult);
                            _logger.LogError(msg);
                            _telegramBotHelper.SendMessage(msg);
                            result.AppendLine(msg);
                        }
                    }

                    if (tpHelper.CanCheckThirdPartyData)
                    {
                        var thirdPartyResult = await tpHelper.CheckThirdPartyData(checkOption, check);
                        if (thirdPartyResult.Any())
                        {
                            var msg = GetMessage(FinanceCheckType.ThirdPartyData, checkOption, thirdPartyResult);
                            _logger.LogError(msg);
                            _telegramBotHelper.SendMessage(msg);
                            result.AppendLine(msg);
                        }
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
            [DisplayData("JobName", ConvertType = typeof(JobList))] string? jobName = null,
            [DisplayData("BeginTime", defaultValue: "2022-03-01")] string? BeginTime = null,
            [DisplayData("EndTime", defaultValue: "2022-03-20")] string? EndTime = null)
        {
            if (!_jobList.Any())
            {
                return null;
            }

            var jobOption = _jobList.First(i => i.JobName == jobName);
            jobOption.Parameters.Update("BeginTime", BeginTime);
            jobOption.Parameters.Update("EndTime", EndTime);

            return ExecuteAsync($"Execute.{jobName}.({BeginTime}~{EndTime})", jobOption, context, cancellationToken).Result;
        }

        private class JobList : IInputDataList
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

        private string GetMessage(FinanceCheckType checkType, FinanceCheckOption checkOption, IEnumerable<FinanceCheckResult> checkResults)
        {
            var result = new StringBuilder();
            result.AppendLine($"[Check{checkType}：{checkOption.BeginTime:yyyy/MM/dd HH:mm} ~ {checkOption.EndTime:yyyy/MM/dd HH:mm}]");
            foreach (var check in checkResults)
            {
                result.AppendLine($"TpId:{check.TpId} | CurrencyId:{check.CurrencyId} | {check.ErrorMessage}");
            }
            return result.ToString();
        }
    }
}
