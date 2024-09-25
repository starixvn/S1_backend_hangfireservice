using System.ComponentModel;
using Hangfire;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using Hangfire.Server;

using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Service;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Serilog.Context;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("RegularJob", queue: "Check")]
    public class RegularJob : BaseJob, IScheduledJob
    {
        private static List<JobOption> _jobList;
        private readonly MySqlClient _mainDbClient;

        public RegularJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            MyJobManager.GetJobGroups()?.TryGetValue("Check", out _jobList);
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");         
            try
            {
                //DB定期排程Job
                var option = JsonConvert.DeserializeObject<RegularOption>(jobOption.Parameters);
                var SpParameters = option.SpParameters != null ? JsonConvert.DeserializeObject<dynamic>(option.SpParameters) : null;
                var result = _mainDbClient.Execute(option.SpName, parameters: SpParameters);
                _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || RegularJob , SpName :{option.SpName} , SpExecute result: {result}.");
                _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} End......");
                return $"SpExecute result: {result}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message, $"{DateTime.Now:HH:mm:ss} || {jobName} exception......");
                return false;
            }
        }
        
        [Job]
        [DisplayName("Run {2} by ManagementPage")]
        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public object Run(PerformContext context = null, IJobCancellationToken cancellationToken = null, 
            [DisplayData("JobName", ConvertType = typeof(JobList))] string jobName = null)
        {
            if (!_jobList.Any())
            {
                return null;
            }
            var jobOption = _jobList.First(i => i.JobName == jobName);

            var result = ExecuteAsync($"Execute.{jobName}", jobOption, context, cancellationToken).Result;


            return result;
        }

        private class JobList : IInputDataList
        {
            public Dictionary<string, string> GetData()
            {
                return _jobList?.Where(i => i.JobName.Contains("RegularJob"))
                    .ToDictionary(f => f.JobName, f => f.JobName);
            }
            public string GetDefaultValue()
            {
                return null;
            }
        }
    }
}