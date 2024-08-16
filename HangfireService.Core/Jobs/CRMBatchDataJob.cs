using Hangfire.Server;
using Hangfire;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using Serilog.Context;
using System.ComponentModel;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using Hangfire.Dashboard.Management.Metadata;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Helpers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("CRMBatchDataJob", queue: "report")]
    public class CRMBatchDataJob : BaseJob, IScheduledJob
    {
        private readonly ReportDBDataLayer _reportDBDataLayer;
        private readonly CRMSFTPConfig _cRMSFTPConfig;

        public CRMBatchDataJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
            _cRMSFTPConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<CRMSFTPConfig>("CRMSFTP");
        }

        public class CRMOption
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
            DateTime currentUtcTime = DateTime.UtcNow;
            DateTime Today = new(currentUtcTime.Year, currentUtcTime.Month, currentUtcTime.Day);
            DateTime startTime = Today.AddDays(-1);
            DateTime endTime = Today.AddSeconds(-1);
            var crmData = await _reportDBDataLayer.GetCrmData(option.SP, startTime, endTime);
            var csv = ToCsv(crmData);

            //SFTP路徑
            string remoteDirectory = $"{currentUtcTime.Year}-{currentUtcTime.Month}-{currentUtcTime.Day}";
            //檔名
            string fileName = $"{option.FileName}_{currentUtcTime.Year}-{currentUtcTime.Month}-{currentUtcTime.Day}.csv";
            if (!string.IsNullOrEmpty(csv))
            {
                SendSFTP(csv, remoteDirectory, fileName);
            }
            return true;
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

                    _logger.LogInformation("文件上傳成功！");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"文件上傳失敗: {ex.Message}");
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
            var header = string.Join(",", properties);

            // 獲取CSV每一行的數據
            var rows = records.Select(record =>
            {
                var values = properties.Select(property => ((IDictionary<string, object>)record)[property]?.ToString() ?? string.Empty);
                return string.Join(",", values);
            });

            // 合併欄位名稱和數據
            var csv = header + Environment.NewLine + string.Join(Environment.NewLine, rows);

            return csv;
        }
    }
}
