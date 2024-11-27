using Hangfire.Server;
using Hangfire;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using Serilog.Context;
using System.ComponentModel;
using Renci.SshNet;
using Hangfire.Dashboard.Management.Metadata;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Helpers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using Hangfire.Dashboard.Management.Support;
using HangfireService.Core.Extensions;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("CRMBatchDataJob", queue: "Report")]
    public class CRMBatchDataJob : BaseJob, IScheduledJob
    {
        private readonly ReportBackupDBDataLayer _reportBackupDBDataLayer;
        private readonly CRMSFTPConfig _cRMSFTPConfig;
        private static List<JobOption> _jobList;

        public CRMBatchDataJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _reportBackupDBDataLayer = provider.GetRequiredService<ReportBackupDBDataLayer>();
            _cRMSFTPConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<CRMSFTPConfig>("CRMSFTP");
            MyJobManager.GetJobGroups()?.TryGetValue("Report", out _jobList);
        }

        public class CRMOption
        {
            public List<CRMDetail> BatchList { get; set; }
            public string Date { get; set; } = string.Empty;
        }

        public class CRMDetail
        {
            public string SP { set; get; }
            public string FileName { get; set; }
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context,
            IJobCancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");
            var option = JsonConvert.DeserializeObject<CRMOption>(jobOption.Parameters);

            DateTime targetTime = string.IsNullOrEmpty(option.Date) ? DateTime.UtcNow : DateTime.ParseExact(option.Date, "yyyy-MM-dd", null);
            DateTime target = new(targetTime.Year, targetTime.Month, targetTime.Day);
            DateTime startTime = target.AddDays(-1);
            DateTime endTime = target.AddSeconds(-1);
            var uploadFileCount = 0;
            foreach (var job in option.BatchList)
            {
                var crmData = await _reportBackupDBDataLayer.GetCrmData(job.SP, startTime, endTime);
                var csv = ToCsv(crmData);

                //SFTP路徑
                string remoteDirectory = $"{targetTime.Year}-{targetTime.Month}-{targetTime.Day}";
                //檔名
                string fileName = $"{job.FileName}_{targetTime.Year}-{targetTime.Month}-{targetTime.Day}.csv";
                if (!string.IsNullOrEmpty(csv))
                {
                    SendSFTP(csv, remoteDirectory, fileName);
                    uploadFileCount++;
                }
            }

            return $"success upload file count: {uploadFileCount}";
        }

        [Job]
        [DisplayName("Run {2} by ManagementPage")]
        public object Run(
            PerformContext context = null,
            IJobCancellationToken cancellationToken = null,
            [DisplayData("JobName", ConvertType = typeof(JobList))] string jobName = null,
            [DisplayData("Date", placeholderText: "yyyy-MM-dd")] string date = null)
        {
            if (!_jobList.Any())
            {
                return null;
            }
            var jobOption = _jobList.First(i => i.JobName == jobName);
            jobOption.Parameters = jobOption.Parameters.Update("Date", date);

            var result = ExecuteAsync($"Execute.{jobName}.{date}", jobOption, context, cancellationToken).Result;

            return result;
        }

        private void SendSFTP(string csv, string remoteDirectory, string fileName)
        {
            using (var sftp = new SftpClient(_cRMSFTPConfig.Url, _cRMSFTPConfig.User, _cRMSFTPConfig.Password))
            {
                try
                {
                    // 連接到 SFTP 服務器
                    sftp.Connect();

                    // 檢查遠程目錄是否存在，不存在則創建
                    if (!sftp.Exists(remoteDirectory))
                    {
                        sftp.CreateDirectory(remoteDirectory);
                    }

                    using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(csv)))
                    {
                        // 上傳 MemoryStream 到 SFTP 服務器
                        sftp.UploadFile(memoryStream, Path.Combine(remoteDirectory, fileName));
                    }

                    _logger.LogInformation($"{remoteDirectory}/{fileName} 文件上傳成功！");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"{remoteDirectory}/{fileName} 文件上傳失敗: {ex.Message}");
                }
                finally
                {
                    // 斷開連結
                    sftp.Disconnect();
                }
            }
        }

        private string ToCsv(IEnumerable<dynamic> records)
        {
            if (records == null || !records.Any())
                return string.Empty;

            //  獲取第一個紀錄的屬性名稱作為CSV欄位名稱
            var firstRecord = records.First();
            var properties = ((IDictionary<string, object>)firstRecord).Keys.ToList();
            var header = string.Join("\t", properties);

            // 獲取CSV每一行的數據
            var rows = records.Select(record =>
            {
                var values = properties.Select(property => ((IDictionary<string, object>)record)[property]?.ToString() ?? string.Empty);
                return string.Join("\t", values);
            });

            // 合併欄位名稱和數據
            var csv = header + Environment.NewLine + string.Join(Environment.NewLine, rows);

            return csv;
        }

        private class JobList : IInputDataList
        {
            public Dictionary<string, string> GetData()
            {
                return _jobList?.Where(i =>
                    i.JobName.Contains("CRMBatchData")
                    ).ToDictionary(f => f.JobName, f => f.JobName);
            }
            public string GetDefaultValue()
            {
                return null;
            }
        }
    }
}
