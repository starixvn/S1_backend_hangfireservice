using System.ComponentModel;
using Hangfire;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using Hangfire.Server;
using HangfireService.Core.Extensions;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Models.Promotion;
using HangfireService.Core.Payment;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;

namespace HangfireService.Core.Jobs
{
    [ManagementPage("PaymentSummaryJob", queue: "relay")]
    public class PaymentSummaryJob : BaseJob, IScheduledJob
    {
        private static List<JobOption> _jobList;
        private readonly IEnumerable<PaymentSummaryBase> _payment;
        public PaymentSummaryJob(IServiceProvider provider) : base(provider)
        {
            LogContext.PushProperty("ClassName", GetType().Name);
            MyJobManager.GetJobGroups()?.TryGetValue("Relay", out _jobList);
            _payment = provider.GetServices<PaymentSummaryBase>();
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");
            try
            {
                var option = JsonConvert.DeserializeObject<PaymentOption>(JsonConvert.SerializeObject(jobOption.Parameters));
                
                var payment = _payment.Single(a => a.WorkerType == option.WorkerType);
                await payment.Run();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return JsonConvert.SerializeObject(ex);
            }
        }
        
        private class JobList : IInputDataList
        {
            public Dictionary<string, string> GetData()
            {
                return _jobList?.Where(i => i.JobName.Contains("PaymentSummary"))
                    .ToDictionary(f => f.JobName, f => f.JobName);
            }

            public string GetDefaultValue()
            {
                return null;
            }
        }

        [Job]
        [DisplayName("Run {2} by ManagementPage")]
        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public async Task<object> Run(
            PerformContext context = null,
            IJobCancellationToken cancellationToken = null,
            [DisplayData("JobName", ConvertType = typeof(JobList))] string jobName = null,
            [DisplayData("BeginDate", placeholderText: "yyyy-MM-dd")] string beginDate = null,
            [DisplayData("EndDate", placeholderText: "yyyy-MM-dd")] string endDate = null,
            [DisplayData("Id", placeholderText: "Id")] string id = null)
        {
            if (!_jobList.Any())
            {
                return null;
            }
            var jobOption = _jobList.First(i => i.JobName == jobName);
            var option = JsonConvert.DeserializeObject<PaymentOption>(JsonConvert.SerializeObject(jobOption.Parameters));

            jobOption.Parameters.Update("Id", id);
            var result = await ExecuteAsync($"Execute.{jobName}.((Id:{id})", jobOption, context, cancellationToken);
            return result;
        }
    }
}