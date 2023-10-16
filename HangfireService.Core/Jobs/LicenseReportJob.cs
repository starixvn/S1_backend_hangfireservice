using System.ComponentModel;
using System.Dynamic;
using System.Net;
using Amazon;
using Amazon.Runtime.Internal;
using Amazon.S3;
using Amazon.S3.Model;
using Hangfire;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using Hangfire.Server;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Extensions;
using HangfireService.Core.Helpers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Models.Options;
using HangfireService.Core.ThirdParty;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog.Context;


namespace HangfireService.Core.Jobs
{
    [ManagementPage("LicenseReportJob", queue: "report")]
    public class LicenseReportJob : BaseJob, IScheduledJob
    {
        private readonly ReportDBDataLayer _reportDbDataLayer;
        private readonly ExcelHelper _excelHelper;
        private readonly string _proxyHost;
        private readonly int _proxyPort;
        private readonly string _awsAccessKey;
        private readonly string _awsSecretKey;

        private readonly BaseTpHelper _baseTpHelper;
        private static List<JobOption> _reportJobList;
        private readonly Dictionary<string, LicenseReportConfig> _licenseReportConfig;

        public LicenseReportJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            _reportDbDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
            _baseTpHelper = provider.GetRequiredService<BaseTpHelper>();
            MyJobManager.GetJobGroups()?.TryGetValue("Report", out _reportJobList);
            _excelHelper = provider.GetRequiredService<ExcelHelper>();

            var configBll = provider.GetRequiredService<ConfigBLL>();
            var proxyConfig = configBll.GetConfigValue<ProxyConfig>("ProxySetting");
            _proxyHost = proxyConfig.ProxyHost;
            _proxyPort = proxyConfig.ProxyPort;

            var awsConfig = configBll.GetConfigValue<AwsConfig>("HangfireAwsKey");
            _awsAccessKey = awsConfig?.AccessKey;
            _awsSecretKey = awsConfig?.SecretKey;

            // ±_ª¬ Json object ³B²z
            var configValue = configBll.GetConfigValue<dynamic>("LicenseReport");
            string jsonString = JsonConvert.SerializeObject(configValue);
            var configResult = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, LicenseReportConfig>>>(jsonString);
            if(configResult == null) return;
            _licenseReportConfig = configResult["LicenseReportConfig"];
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");

                var date = GetLastDayOfMonth(DateTime.UtcNow.AddHours(8).AddMonths(-1));
                var jobResult = new JobResult();
                foreach (var config in _licenseReportConfig)
                {
                    var data = _reportDbDataLayer.GetBalanceForLicense((int)SiteId.Nova88, date,
                        JsonConvert.SerializeObject(config.Value.CurrencyId));
                    var stream = _excelHelper.Export(data, "1qaz#EDC2wsx$RFV");
                    var name = $"{date.Year}{date.Month.ToString("00")}_{config.Key}";

                    var s3Result = UploadS3FileAsync(new MemoryStream(stream), $"{name}.csv");
                    if (s3Result.Result)
                    {
                        jobResult.UploadS3Success++;
                    }
                    else
                    {
                        jobResult.UploadS3Fail++;
                    }

                    var result = await _baseTpHelper.HttpPostAsync(config.Value.UploadUrl, stream, name, "text/csv");
                    await using var s = await result.Content.ReadAsStreamAsync();
                    using var sr = new StreamReader(s);
                    using JsonReader reader = new JsonTextReader(sr);
                    var serializer = new JsonSerializer();
                    var response = serializer.Deserialize<CuracaoResponse>(reader);

                    if (response.status != 200)
                    {
                        _logger.LogError($"Upload License File fail, Response={response}");
                        jobResult.UploadFail++;
                    }
                    else
                    {
                        jobResult.UploadSuccess++;
                    }
                }

                if (jobResult.UploadFail > 0)
                {
                    BackgroundJob.Schedule(() => ExecuteAsync(jobName, jobOption, null, null), TimeSpan.FromMinutes(5));
                }

                return JsonConvert.SerializeObject(jobResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LicenseReportJob");
                BackgroundJob.Schedule(() => ExecuteAsync(jobName, jobOption, null, null), TimeSpan.FromMinutes(5));
                return JsonConvert.SerializeObject(ex);
            }

        }

        private static DateTime GetLastDayOfMonth(DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, DateTime.DaysInMonth(dateTime.Year, dateTime.Month));
        }

        private async Task<bool> UploadS3FileAsync(MemoryStream stream, string name)
        {
            try
            {
                var config = new AmazonS3Config
                {
                    RegionEndpoint = RegionEndpoint.APSoutheast1
                };

                if (!string.IsNullOrEmpty(_proxyHost) && _proxyPort != 0)
                {
                    config.ProxyHost = _proxyHost;
                    config.ProxyPort = _proxyPort;
                }

                IAmazonS3 client = new AmazonS3Client(_awsAccessKey, _awsSecretKey, config);
                var putRequest = new PutObjectRequest
                {
                    BucketName = "ailab-license",
                    ContentType = "text/csv",
                    InputStream = stream,
                    Key = name
                };

                var putResponse = await client.PutObjectAsync(putRequest);

                if (putResponse.HttpStatusCode != HttpStatusCode.OK)
                {
                    _logger.LogError($"UploadS3File, Response={JsonConvert.SerializeObject(putResponse)}");
                }

                return putResponse.HttpStatusCode == HttpStatusCode.OK;
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, MethodExtension.GetCurrentAsyncMethod());
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, MethodExtension.GetCurrentAsyncMethod());
                return false;
            }
        }

        private class JobList : IInputDataList
        {
            public Dictionary<string, string> GetData()
            {
                return _reportJobList?.Where(i => i.JobName.Contains("LicenseReport"))
                    .ToDictionary(f => f.JobName, f => f.JobName);
            }

            public string GetDefaultValue()
            {
                return null;
            }
        }

        [Job]
        [DisplayName("Run {2} by ManagementPage")]
        public object Run(
            PerformContext context = null,
            IJobCancellationToken cancellationToken = null,
            [DisplayData("JobName", ConvertType = typeof(JobList))]
            string jobName = null)
        {
            if (!_reportJobList.Any())
            {
                return null;
            }

            var jobOption = _reportJobList.First(i => i.JobName == jobName);

            return ExecuteAsync($"Execute.{jobName}", jobOption, context, cancellationToken).Result;
        }
    }
}