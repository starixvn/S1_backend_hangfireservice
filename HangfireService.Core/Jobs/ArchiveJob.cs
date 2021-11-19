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

namespace HangfireService.Core.Jobs
{
    [ManagementPage("ArchiveJob", queue:"archive")]
    public class ArchiveJob : BaseJob, IScheduledJob
    {
        private readonly MainDBDataLayer _mainDBDataLayer;

        private readonly ReportDBDataLayer _reportDBDataLayer;

        private static List<JobOption> _archiveJobList = new();

        public ArchiveJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);         
            _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();         
            _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
      
            var jobGroups = provider.GetRequiredService<Dictionary<string,List<JobOption>>>();
            jobGroups.TryGetValue("Archive", out _archiveJobList);      
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            try
            {
                var totalResult = 0;
                var option = JsonConvert.DeserializeObject<ArchiveOption>(JsonConvert.SerializeObject(jobOption.Parameters));

                // 設定執行日期與時間
                var date = GetUTC8Time().ToString("yyyy/MM/dd");
                if (!DateTime.TryParse($"{date} {option.StartTime}", out var startTime)
                    || !DateTime.TryParse($"{date} {option.EndTime}", out var endTime))
                {
                    return JsonConvert.SerializeObject(new
                    {
                        Success = false,
                        Message = $"{jobName} || time format error. || Date:{date} StartTime:{option.StartTime} EndTime:{option.EndTime}"
                    });
                }

                _logger.LogInformation($"{jobName} || {date} {startTime:HH:mm}～{endTime:HH:mm} ");
                if (endTime < startTime)
                {
                    endTime = endTime.AddDays(1);
                }

                var meassage = string.Empty;
                while (true)
                {
                    var sw = Stopwatch.StartNew();
                    var batchResult = 0;
                    var beginAt = GetUTC8Time();
                    if (!beginAt.IsBetween(startTime, endTime, true))
                    {
                        meassage = $"{beginAt:yyyy/MM/dd HH:mm} is not in the StartTime and EndTime.";
                        break;
                    }
                    
                    var data = _reportDBDataLayer.GetArchiveData(option.GetSP, option.LiminNum).ToList();
                    if (data.Count < option.LiminNum)
                    {
                        meassage = $"archive data({data.Count}) is less than LiminNum({option.LiminNum})";
                        break;
                    }

                    Parallel.ForEach(data, item =>
                    {
                        var trans = _mainDBDataLayer.BeginTransaction();
                        var insertResult = _mainDBDataLayer.ExecuteSP(option.InsertSP, item, trans);
                        if (insertResult > 0)
                        {
                            var deleteResult = _mainDBDataLayer.ExecuteSP(option.DeleteSP, item, trans);
                            if (deleteResult > 0)
                            {
                                Interlocked.Add(ref batchResult, deleteResult);
                                trans.Commit();                                           
                            }                      
                        }
                        trans.Connection?.Close();
                    });

                    _logger.LogInformation($"{jobName} || archive {batchResult} data || {sw.Elapsed:c}");
                    totalResult += batchResult;

                    cancellationToken.ThrowIfCancellationRequested();
                    Thread.Sleep(option.ColdDownTime * 1000);
                }

                if (totalResult > 0)
                {
                    meassage = $"archive {totalResult} data complete.";
                }

                return JsonConvert.SerializeObject(new
                {
                    Success = true,
                    Count = totalResult,
                    Message = $"{jobName} || {meassage} || {date} {startTime:HH:mm}～{endTime:HH:mm}"
                });
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, $"{jobName} || {ex.Message}");
                return JsonConvert.SerializeObject(new
                {
                    Success = false,
                    Message = $"{jobName} ||{ex.Message}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{jobName} || {ex.Message}");
                return JsonConvert.SerializeObject(ex);
            }
        }

        [Job]
        [DisplayName("Run {2} by ManagementPage")]
        public object Run(
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
            if (!_archiveJobList.Any())
            {
                return null; 
            }

            var jobOption = _archiveJobList.First(i => i.JobName == jobName);
            jobOption.Parameters.Update("StartTime", startTime);
            jobOption.Parameters.Update("EndTime", endTime);
            jobOption.Parameters.Update("LiminNum", liminNum);
            jobOption.Parameters.Update("ColdDownTime", coldDownTime);

            return ExecuteAsync($"Execute {jobName}", jobOption, context, cancellationToken).Result;
        }

        private DateTime GetUTC8Time()
        {
            return new DateTimeOffset(DateTime.UtcNow).ToOffset(new TimeSpan(8, 0, 0)).DateTime;
        }

        private class ArchiveJobList : IInputDataList
        {      
            public Dictionary<string, string>? GetData()
            {
                return _archiveJobList?.ToDictionary(f => f.JobName, f => f.JobName);
            }

            public string? GetDefaultValue()
            {
                return null;
            }
        }
    }
}
