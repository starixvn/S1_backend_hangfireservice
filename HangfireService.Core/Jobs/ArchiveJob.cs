using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Newtonsoft.Json;
using System.ComponentModel;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Extensions;
using Hangfire.Server;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using Hangfire;
using MySql.Data.MySqlClient;
using System.Collections.Concurrent;
using Hangfire.Dashboard.Management.Pages;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("ArchiveJob", queue: "archive")]
    public class ArchiveJob : BaseJob, IScheduledJob
    {
        private readonly MainDBDataLayer _mainDBDataLayer;

        private readonly ReportDBDataLayer _reportDBDataLayer;

        private static List<JobOption> _archiveJobList = new();

        private static readonly ConcurrentDictionary<string, string> _JobStateList = new();

        public ArchiveJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
            _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();

            var jobGroups = provider.GetRequiredService<Dictionary<string, List<JobOption>>>();
            jobGroups.TryGetValue("Archive", out _archiveJobList);
        }


        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            var message = string.Empty;
            try
            {
                // 處理透過web介面手動新增的RecurringJob名稱
                if (context.BackgroundJob.Job.ToString().Equals("ArchiveJob.Run", StringComparison.OrdinalIgnoreCase))
                {
                    jobName = context.Connection.GetRecurringJobs().First(item => item.LastJobId == context.BackgroundJob.Id).Id;
                }

                message += $"{jobName} || ";

                if (IsProcessing(jobName, context.BackgroundJob.Id, out var lastjobId))
                {
                    message += $"{lastjobId} is Processing.";
                    _logger.LogWarning(message);
                    return message;
                }

                var option = JsonConvert.DeserializeObject<ArchiveOption>(JsonConvert.SerializeObject(jobOption.Parameters));
                var archiveList = _reportDBDataLayer.GetArchiveList(option.GetSP, option.LiminNum);
                
                var success = 0;
                Parallel.ForEach(archiveList,
                    new ParallelOptions() { MaxDegreeOfParallelism = option.TaskCount },
                    async (data) =>
                    {
                        int excuteResult = await ExcuteArchive(option, data);
                        Interlocked.Add(ref success, excuteResult);
                        cancellationToken.ThrowIfCancellationRequested();
                    });

                if (PauseCheck(archiveList.Count(), option.LiminNum, success, out var reason))
                {
                    message += $"{reason} || ";
                    Pause(jobName, reason, option.PauseHours);
                }

                if (success > 0)
                {
                    message += $"archive {success} data complete.";
                }

                _logger.LogInformation(message);
            }
            catch (OperationCanceledException ex)
            {
                message += $" || {ex.Message}";
                _logger.LogWarning(ex, message);
            }
            catch (Exception ex)
            {
                message += $" || {ex.Message}";
                _logger.LogError(ex, message);
            }
            finally
            {
                _JobStateList.TryRemove(jobName, out _);
            }
            return message;
        }

        [Job]
        [DisplayName("Run {2} by ManagementPage")]
        public object Run(
            PerformContext? context = null,
            IJobCancellationToken? cancellationToken = null,
            [DisplayData("JobName", ConvertType = typeof(ArchiveJobList))]
            string? jobName = null,
            [DisplayData("LiminNum", defaultValue:"1000")]
            string liminNum = "1000",
            [DisplayData("TaskCount", defaultValue:"2")]
            string taskCount = "2")
        {
            if (!_archiveJobList.Any())
            {
                return null;
            }

            var jobOption = _archiveJobList.First(i => i.JobName == jobName);
            jobOption.Parameters.Update("LiminNum", liminNum);
            jobOption.Parameters.Update("TaskCount", taskCount);

            return ExecuteAsync($"Execute.{jobName}.({liminNum}/{taskCount})", jobOption, context, cancellationToken).Result;
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

        private async Task<int> ExcuteArchive(ArchiveOption option, dynamic data)
        {
            using var conn = _mainDBDataLayer.GetMySqlConnection();
            try
            {
                conn.Open();
                var trans = conn.BeginTransaction();

                var doDelete = string.IsNullOrEmpty(option.InsertSP); //InsertSP空白為不需要搬資料，僅刪除舊資料

                if (!string.IsNullOrEmpty(option.InsertSP))
                {
                    var insertResult = _mainDBDataLayer.InsertArchive(option.InsertSP, data, trans);
                    doDelete = insertResult > 0;
                }

                if (doDelete)
                {
                    var deleteResult = _mainDBDataLayer.DeleteArchive(option.DeleteSP, data, trans);
                    if (deleteResult > 0)
                    {
                        trans.Commit();
                        return deleteResult;
                    }
                }
                return 0;
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, ex.Message);
                if (ex.Number == (int)MySqlErrorCode.DuplicateKeyEntry) //匯入資料重複
                {
                    _mainDBDataLayer.DeleteArchive(option.DeleteSP, data);
                }
                return 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return 0;
            }
        }

        private static bool IsProcessing(string jobName, string jobId, out string? lastjobId)
        {
            if (_JobStateList.TryGetValue(jobName, out lastjobId))
            {
                return true;
            }

            _JobStateList.TryAdd(jobName, jobId);
            return false;
        }

        private static bool PauseCheck(int archiveData, int liminNum, int success, out string reason)
        {
            reason = string.Empty;
            //小於批次數量暫停
            if (archiveData < liminNum)
            {
                reason = $"archive data({archiveData}) is less than LiminNum({liminNum})";
                return true;
            }

            //批次數量全部失敗暫停
            if (archiveData > 0 && success == 0)
            {
                reason = $"archive data({archiveData}) all fail.";
                return true;
            }

            return false;
        }

        private void Pause(string jobName, string reason, int pauseHours)
        {
            _logger.LogWarning($"{jobName} restart after {pauseHours} hours, because {reason}.");
            JobManager.PauseThenRestart(jobName, pauseHours, reason);
        }
    }
}
