using Hangfire.Server;
using Hangfire;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Service;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.ComponentModel;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using Newtonsoft.Json;
using HangfireService.Core.Models.AWS;

namespace HangfireService.Core.Jobs.AWS;

[ManagementPage("AWSS3CredentialJob", queue: "aws")]
internal class AWSS3CredentialJob : BaseJob, IScheduledJob
{
    private readonly AWSS3CredebtialJobService _aWSS3CredebtialJobService;
    private static List<JobOption> _jobList;

    public AWSS3CredentialJob(IServiceProvider provider) : base(provider)
    {
        LogContext.PushProperty("ClassName", GetType().Name);
        MyJobManager.GetJobGroups()?.TryGetValue("AWS", out _jobList);
        _aWSS3CredebtialJobService = provider.GetRequiredService<AWSS3CredebtialJobService>();
    }

    [DisplayName("{0}")]
    public async Task<object> ExecuteAsync(string jobName,
        JobOption jobOption,
        PerformContext context,
        IJobCancellationToken cancellationToken)
    {
        _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");
        try
        {
            var option = JsonConvert.DeserializeObject<AWSS3CredebtialOption>(jobOption.Parameters);
            int keyId = option.KeyId;
            if (option.Type == 0)
            {
                await _aWSS3CredebtialJobService.RunCheckJobAsync(keyId);
            }
            else
            {
                await _aWSS3CredebtialJobService.RunUpdateJobAsync(keyId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message, $"{DateTime.Now:HH:mm:ss} || {jobName} exception......");
            return false;
        }
        _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} End......");
        return true;
    }

    [Job]
    [DisplayName("Run {2} by ManagementPage")]
    [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task<object> Run(
        PerformContext context = null,
        IJobCancellationToken cancellationToken = null,
        [DisplayData("JobName", ConvertType = typeof(JobList))] string jobName = null)
    {
        if (!_jobList.Any())
        {
            return null;
        }
        var jobOption = _jobList.First(i => i.JobName == jobName);
        var option = JsonConvert.DeserializeObject<AWSS3CredebtialOption>(jobOption.Parameters);
        option.Type = 1;
        jobOption.Parameters = JsonConvert.SerializeObject(option);
        return await ExecuteAsync($"Execute.{jobName}.((Type:1,KeyId:{option.KeyId})", jobOption, context, cancellationToken);

    }

    private class JobList : IInputDataList
    {
        public Dictionary<string, string> GetData()
        {
            return _jobList?.Where(i => i.JobName.Contains("AWSS3Credential"))
                .ToDictionary(f => f.JobName, f => f.JobName);
        }

        public string GetDefaultValue()
        {
            return null;
        }
    }
}
