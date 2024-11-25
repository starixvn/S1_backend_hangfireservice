using System.ComponentModel;
using System.Text;
using Hangfire;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using Hangfire.Server;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Extensions;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Finance;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Models.Options;
using HangfireService.Core.ThirdParty.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("FinanceCheckTransIdJob", queue: "check")]
    internal class FinanceCheckTransIdJob : BaseJob, IScheduledJob
    {
        private readonly ReportDBDataLayer _reportDBDataLayer;
        private readonly MainDBDataLayer _mainDBDataLayer;

        private readonly Dictionary<int, IFinanceTpHelper> _tpHandler = new();

        private static List<JobOption> _jobList;

        public FinanceCheckTransIdJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            MyJobManager.GetJobGroups()?.TryGetValue("Check", out _jobList);

            _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
            _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
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
                    if (tpId == 36 || tpId == 37 || tpId == 38 || tpId == 59)
                    {
                        tpBetRecordList = tpBetRecordList.Where(x => x.TicketStatus != 6 || x.TicketStatus != 3);
                    }
                    else if (tpId == 6)
                    {
                        tpBetRecordList = tpBetRecordList.Where(x => x.SettlementType != 0);
                    }
                    else if (tpId == 42)
                    {
                        tpBetRecordList = tpBetRecordList.Where(x => x.TicketStatus != 3);
                    }
                    var bettransList = await tpHelper.GetBettransCheckTransIdData(checkOption);
                    var checkResult = tpHelper.CheckTransIdData(tpId, checkOption, tpBetRecordList.ToList(), bettransList.ToList());
                    if (checkResult.Any())
                    {
                        // 寫入需重新確認的資料
                        InsertFinanceCheckTransId(tpId, checkOption, checkResult);
                        var msg = GetMessage(tpId, checkOption, checkResult);
                        //_logger.LogError(msg);
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
            [DisplayData("CheckTime", placeholderText: "yyyy-MM-dd HH:00:00")] string CheckTime = null
        )
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
                result.AppendLine($"[{checkOption.BeginTime:yyyy-MM-dd HH:00:00}] TpId:{TpId} The account number check is normal");
                _logger.LogInformation(result.ToString());
                return result.ToString();
            }

            result.AppendLine($"[{checkOption.BeginTime:yyyy-MM-dd HH:00:00}] TpId:{TpId} The account number check is abnormal");
            foreach (var check in checkResults)
            {
                result.AppendLine($"\u3000\u3000{check.FinanceCheckTransIdType}");

                if (check.FinanceCheckTransIdType == FinanceCheckTransIdType.結算與交易資料檢查)
                {
                    foreach (var error in check.ErrorList)
                    {
                        result.AppendLine($"\u3000\u3000\u3000 - {error}");
                        _logger.LogError($"\r\n\u3000\u3000\u3000 - {error}");
                    }
                }
                else if (check.FinanceCheckTransIdType == FinanceCheckTransIdType.結算與交易金額檢查)
                {
                    _logger.LogError($"{check.FinanceCheckTransIdType}-Abnormal data TpId:{TpId},Abnormal count: {check.Count}\r\n{check.ErrorData}");
                }
                else
                {
                    _logger.LogError($"{check.FinanceCheckTransIdType}-Abnormal data TpId:{TpId},Abnormal entries: {check.Count}\r\n{JsonConvert.SerializeObject(check.ErrorList)}");
                }
            }
            return result.ToString();
        }

        private bool InsertFinanceCheckTransId(int TpId, FinanceCheckTransIdOption checkOption, IEnumerable<FinanceCheckTransIdResult> checkResults)
        {
            var result = new StringBuilder();
            var checkFilter = checkResults.Where(i => i.ErrorList.Any());

            if (!checkFilter.Any())
            {
                return false;
            }

            foreach (var check in checkResults)
            {
                var financeCheckTransIdData = new FinanceCheckTransIdData
                {
                    TpId = TpId,
                    TransId = "",
                    FinanceCheckTransIdType = (int)check.FinanceCheckTransIdType,
                    CheckTime = checkOption.BeginTime,
                    ErrorData = "[]"
                };

                if (check.FinanceCheckTransIdType == FinanceCheckTransIdType.結算與交易資料檢查)
                {
                    foreach (var error in check.ErrorList)
                    {
                        financeCheckTransIdData.ErrorData = error;
                        _mainDBDataLayer.InsertFinanceCheckTransId(financeCheckTransIdData);
                    }
                }
                else if (check.FinanceCheckTransIdType == FinanceCheckTransIdType.結算與交易金額檢查)
                {
                    foreach (var error in check.ErrorList)
                    {
                        var errorDetail = JsonConvert.DeserializeObject<List<ErrorDetail>>(error);
                        foreach (var detail in errorDetail)
                        {
                            financeCheckTransIdData.TransId = detail.TransId;
                            financeCheckTransIdData.ErrorData = JsonConvert.SerializeObject(detail);
                            _mainDBDataLayer.InsertFinanceCheckTransId(financeCheckTransIdData);
                        }
                    }
                }
                else if (check.FinanceCheckTransIdType == FinanceCheckTransIdType.結算與交易單號檢查 || check.FinanceCheckTransIdType == FinanceCheckTransIdType.交易與結算單號檢查)
                {
                    foreach (var error in check.ErrorList)
                    {
                        financeCheckTransIdData.TransId = error;
                        financeCheckTransIdData.ErrorData = check.ErrorData;
                        _mainDBDataLayer.InsertFinanceCheckTransId(financeCheckTransIdData);
                    }
                }
                else
                {
                    financeCheckTransIdData.ErrorData = JsonConvert.SerializeObject(check.ErrorList);
                    _mainDBDataLayer.InsertFinanceCheckTransId(financeCheckTransIdData);
                }
            }

            return true;
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
