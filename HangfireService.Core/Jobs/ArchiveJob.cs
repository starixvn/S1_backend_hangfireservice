using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Newtonsoft.Json;
using System.ComponentModel;
using HangfireService.Core.Models.Options;
using System.Diagnostics;
using HangfireService.Core.Extensions;
using Hangfire.Server;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using Hangfire;
using Microsoft.Extensions.Configuration;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("Archive", queue:"archive")]
    public class ArchiveJob : BaseJob, IScheduledJob
    {
        private readonly MainDBDataLayer _mainDBDataLayer;

        private readonly ReportDBDataLayer _reportDBDataLayer;

        private readonly  IConfiguration _configuration;

        private static List<JobOption> _archiveJobList;


        public ArchiveJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
            _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
            _configuration = provider.GetRequiredService<IConfiguration>();

            var jobGroups = new Dictionary<string, List<JobOption>>();
            _configuration.GetSection("ScheduledJob").Bind(jobGroups);

            if (jobGroups.TryGetValue("Archive", out var jobGroupSetting))
            {
                _archiveJobList = JsonConvert.DeserializeObject<List<JobOption>>(JsonConvert.SerializeObject(jobGroupSetting));
            }
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            var jobId = context.BackgroundJob.Id;
            LogContext.PushProperty("jobId", jobId);

            try
            {
                var totalResult = 0;
                var option = JsonConvert.DeserializeObject<ArchiveOption>(JsonConvert.SerializeObject(jobOption.Parameters));

                // 設定執行日期與時間
                var beginAt = GetUTC8Time();
                if (!DateTime.TryParse($"{beginAt:yyyy/MM/dd} {option.StartTime}", out var startTime)
                    || !DateTime.TryParse($"{beginAt:yyyy/MM/dd} {option.EndTime}", out var endTime))
                {
                    return JsonConvert.SerializeObject(new
                    {
                        Success = false,
                        Message = $"{jobName} {jobId} || time format error. || beginAt:{beginAt:yyyy/MM/dd} StartTime:{option.StartTime} EndTime:{option.EndTime}"
                    });
                }

                _logger.LogInformation($"{jobName} {jobId} || {beginAt:yyyy/MM/dd} {startTime:HH:mm}～{endTime:HH:mm} ");
                if (endTime < startTime)
                {
                    endTime = endTime.AddDays(1);
                }

                while (GetUTC8Time().IsBetween(startTime, endTime, true))
                {
                    var sw = Stopwatch.StartNew();
                    var batchResult = 0;
                    var data = _reportDBDataLayer.GetArchiveData(option.GetSP, option.LiminNum).ToList();
                    if (data.Count < option.LiminNum)
                    {
                        break;
                    }

                    Parallel.ForEach(data, item =>
                    {
                        var insertResult = _mainDBDataLayer.ExecuteSP(option.InsertSP, item);
                        if (insertResult > 0)
                        {
                            var deleteResult = _mainDBDataLayer.ExecuteSP(option.DeleteSP, item);
                            Interlocked.Add(ref batchResult, deleteResult);
                        }
                    });

                    _logger.LogInformation($"{jobName} {jobId} || archive {batchResult} data || {sw.Elapsed:c}");
                    totalResult += batchResult;

                    cancellationToken.ThrowIfCancellationRequested();
                    Thread.Sleep(option.ColdDownTime * 1000);
                }

                return JsonConvert.SerializeObject(new
                {
                    Success = true,
                    Count = totalResult,
                    Message = $"{jobName} {jobId} || archive {totalResult} data complete."
                });
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, $"{jobName} {jobId} || {ex.Message}");
                return JsonConvert.SerializeObject(new
                {
                    Success = false,
                    Message = $"{jobName} {jobId} ||{ex.Message}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{jobName} {jobId} || {ex.Message}");
                return JsonConvert.SerializeObject(ex);
            }
        }

        [Job]
        [DisplayName("Execute Archive.{2} by ManagementPage")]
        public void Action(
            PerformContext context = null,
            IJobCancellationToken cancellationToken = null,
            [DisplayData("JobName", ConvertType = typeof(ArchiveJobList))]
            string jobName = null,
            [DisplayData("StartTime", defaultValue:"00:00")]
            string startTime= "00:00",
            [DisplayData("EndTime", defaultValue:"23:59")]
            string endTime= "23:59",
            [DisplayData("LiminNum", defaultValue:"1000")]
            string liminNum = "1000",
            [DisplayData("ColdDownTime", defaultValue:"1")]
            string coldDownTime = "1")
        {
            if (_archiveJobList.Any())
            {
                var jobOption = _archiveJobList.First(i => i.JobName == jobName);
                jobOption.Parameters.Update("StartTime", startTime);
                jobOption.Parameters.Update("EndTime", endTime);
                jobOption.Parameters.Update("LiminNum", liminNum);
                jobOption.Parameters.Update("ColdDownTime", coldDownTime);

                _ = ExecuteAsync($"Execute {jobName}", jobOption, context, cancellationToken);
            }
        }

        private DateTime GetUTC8Time()
        {
            return new DateTimeOffset(DateTime.Now).ToOffset(new TimeSpan(8, 0, 0)).DateTime;
        }

        private class ArchiveJobList : IInputDataList
        {      
            public Dictionary<string, string> GetData()
            {
                return _archiveJobList.ToDictionary(f => f.JobName, f => f.JobName);
            }

            public string GetDefaultValue()
            {
                return null;
            }
        }
    }
}
