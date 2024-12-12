using System.ComponentModel;
using System.Diagnostics;
using Hangfire;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Server;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Extensions;
using HangfireService.Core.Helpers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Models.Options;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Serilog.Context;

namespace HangfireService.Core.Jobs;

[ManagementPage("ArchiveJob", queue: "archive")]
public class AllArchiveJob : BaseJob, IScheduledJob
{
    private readonly MainDBDataLayer _mainDBDataLayer;

    private readonly ReportDBDataLayer _reportDBDataLayer;

    private readonly TelegramBotHelper _telegramBotHelper;

    private readonly ICacheHelper _redisHelper;

    public AllArchiveJob(IServiceProvider provider) : base(provider)
    {
        LogContext.PushProperty("ClassName", GetType().Name);
        _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
        _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
        _redisHelper = provider.GetRequiredService<ICacheHelper>();
        _telegramBotHelper = provider.GetRequiredService<TelegramBotHelper>();
    }

    [DisplayName("{0}")]
    public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var message = $"{jobName} ||";

        var lockKey = $"AllArchiveJob";
        var lockValue = context.BackgroundJob.Id; // 每次取鎖使用唯一值，確保為同一個執行序所執行
        var lockExpiry = TimeSpan.FromMinutes(2); //lock 2 mins   
        if (!_redisHelper.TryCreateLock(lockKey, lockValue, lockExpiry, RedisDataBase.Shared))
        {
            return EndJob($"{message} Job is lock.");
        }

        try
        {
            var option = JsonConvert.DeserializeObject<AllArchiveOption>(jobOption.Parameters);

            // 檢查是否在DB離峰時間
            var taipeiLocalTime = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8)).DateTime;
            if (!taipeiLocalTime.IsBetween(option.SafeRangeStartTime, option.SafeRangeEndTime))
            {
                return EndJob($"{message} Database is busy.");
            }

            // 判斷目前要搬哪個Archive
            var (archiveDetail, archiveList) = GetArchiveData(option, out bool isContinue);
            if (archiveDetail == null)
            {
                return EndJob($"{message} archive data complete.");
            }

            //執行Archive作業
            var success = 0;
            foreach (var item in archiveList)
            {
                var excuteResult = await ExcuteArchive(archiveDetail, item);
                success += excuteResult;
            }

            //判斷是否啟動下一次Job
            if (isContinue)
            {
                BackgroundJob.Enqueue(() => ExecuteAsync(jobName, jobOption, null, null));
            }

            if (success > 0)
            {
                message += $" {archiveDetail.ArchiveName} archive {success} data complete. TotalSeconds:{sw.Elapsed.TotalSeconds}.";
            }
        }
        catch (OperationCanceledException ex)
        {
            message += $" || {ex.Message}";
            _logger.LogWarning(ex, message);
        }
        catch (Exception ex)
        {
            BackgroundJob.Schedule(() => ExecuteAsync(jobName, jobOption, null, null), TimeSpan.FromSeconds(15));
            message += $" || {ex.Message}";
            _logger.LogError(ex, message);
        }
        finally
        {
            _redisHelper.UnLock(lockKey, lockValue, RedisDataBase.Shared);
        }
        return EndJob(message);

        string EndJob(string message)
        {
            _logger.LogInformation(message);
            _telegramBotHelper.SendMessage($"JobName:AllArchiveJob - Message: { message }");
            return message;
        }
    }

    private (ArchiveDetail archiveDetail, IEnumerable<dynamic> archiveList) GetArchiveData(AllArchiveOption option, out bool isContinue)
    {
        isContinue = false;
        var redisKey = "GetArchiveData";
        var redisCacheTime = TimeSpan.FromMinutes(5);
    
        try
        {
            // 先檢查Redis有沒有正在搬移的資料
            if (_redisHelper.KeyExists(redisKey, RedisDataBase.Shared))
            {
                var archiveName = _redisHelper.StringGet(redisKey, RedisDataBase.Shared);
                var archiveDetail = option.ArchiveList.Find(i => i.ArchiveName == archiveName) ?? option.ArchiveList.First();
                var archiveList = GetArchiveList(option, archiveDetail);
                if (archiveList.Count() == option.LiminNum)
                {
                    isContinue = true;
                    _redisHelper.StringSet(redisKey, archiveName, redisCacheTime, RedisDataBase.Shared);
                    return (archiveDetail, archiveList);
                }
            }

            // Redis沒資料依照設定檔順序，檢查要搬移的資料
            foreach (var archive in option.ArchiveList)
            {
                var archiveList = GetArchiveList(option, archive);
                if (archiveList.Count() == option.LiminNum)
                {
                    isContinue = true;
                    _redisHelper.StringSet(redisKey, archive.ArchiveName, redisCacheTime, RedisDataBase.Shared);
                    return (archive, archiveList);
                }
            }

            return (default, default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return (default, default);
        }
    }

    private IEnumerable<dynamic> GetArchiveList(AllArchiveOption option, ArchiveDetail archiveDetail)
    {
        var archiveTime = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8)).AddDays(archiveDetail.ArchiveDays * -1);
        var time = archiveDetail.ArchiveType == "Date" ? archiveTime.Date : archiveTime.DateTime;
        var archiveList = _reportDBDataLayer.GetArchiveList(archiveDetail.GetSP, option.LiminNum, time);
        return archiveList;
    }

    private async Task<int> ExcuteArchive(ArchiveDetail detail, dynamic data)
    {
        await using var conn = _mainDBDataLayer.GetMySqlConnection();
        try
        {
            conn.Open();
            var trans = await conn.BeginTransactionAsync();

            var doDelete = string.IsNullOrEmpty(detail.InsertSP); //InsertSP空白為不需要搬資料，僅刪除舊資料

            if (!string.IsNullOrEmpty(detail.InsertSP))
            {
                var insertResult = _mainDBDataLayer.InsertArchive(detail.InsertSP, data, trans);
                doDelete = insertResult > 0;
            }

            if (doDelete)
            {
                var deleteResult = _mainDBDataLayer.DeleteArchive(detail.DeleteSP, data, trans);
                if (deleteResult > 0)
                {
                    trans.Commit();
                    await conn.CloseAsync();
                    await conn.DisposeAsync();
                    return deleteResult;
                }
            }
            await conn.CloseAsync();
            await conn.DisposeAsync();
            return 0;
        }
        catch (MySqlException ex)
        {
            if (ex.Number == (int)MySqlErrorCode.DuplicateKeyEntry) //匯入資料重複
            {
                var deleteResult = _mainDBDataLayer.DeleteArchive(detail.DeleteSP, data);
                return deleteResult;
            }
            _logger.LogError(ex, ex.Message);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return 0;
        }
    }
}
