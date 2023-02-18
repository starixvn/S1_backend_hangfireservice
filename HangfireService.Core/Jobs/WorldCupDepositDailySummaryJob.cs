using System.ComponentModel;
using System.Dynamic;
using Hangfire;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using Hangfire.Server;
using HangfireService.Core.Extensions;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Service;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HangfireService.Core.Jobs
{
    /// <summary>
    /// 世界杯，計算玩家每日充值金額，與派發票券
    /// </summary>
    [ManagementPage("depositDailySummaryJob", queue: "default")]
    public class WorldCupDepositDailySummaryJob : BaseJob, IScheduledJob
    {
        private readonly WorldCupDepositDailySummaryService _worldCupDepositDailySummaryService;
        
        public WorldCupDepositDailySummaryJob(IServiceProvider provider) : base(provider)
        {
            this._worldCupDepositDailySummaryService = provider.GetRequiredService<WorldCupDepositDailySummaryService>();
        }

        [DisplayName("{0}")]
        public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} running......");
            var option = JsonConvert.DeserializeObject<WorldCupDepositDailySummaryOption>(jobOption.Parameters);
            option.AccountDate = option.AccountDate ?? DateTime.Now.AddHours(8).Date; //機器上是UTC + 0 ，所以要+8小時，如果沒有指定重跑日期，就是抓現在時間
            try
            {
                await _worldCupDepositDailySummaryService.RunJobAsync(option);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message, $"{DateTime.Now:HH:mm:ss} || {jobName} exception......");
                await _worldCupDepositDailySummaryService.InsertJobLog(jobName, WorldCupJobStatus.Fail, option);
                return false;
            }

            
            _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} End......");
            await _worldCupDepositDailySummaryService.InsertJobLog(jobName, WorldCupJobStatus.Success, option);
            return $"true，Date：{option.AccountDate}";
        }

        /// <summary>
        /// 手動重跑
        /// </summary>
        /// <param name="siteId"></param>
        /// <param name="currencyId"></param>
        /// <param name="custId"></param>
        /// <param name="AccountDate"></param>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Job]
        [DisplayName("ReRunWorldCupDepositDailySummaryJob")]
        public object Run([DisplayData("SiteId")]
            int siteId,
            [DisplayData("CurrencyId")]
            int currencyId,
            [DisplayData("CustId")]
            int custId,
            [DisplayData("AccountDate")]
            string AccountDate,
            PerformContext? context = null,
            IJobCancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrEmpty(AccountDate)) return null;
            var result = _worldCupDepositDailySummaryService.ReSetWorldCupDepositDailySummary(siteId, currencyId, custId, DateTime.Parse(AccountDate)).Result;

            var jobOption = new JobOption();
            jobOption.Parameters = jobOption.Parameters.Update("SiteId", siteId.ToString());
            jobOption.Parameters = jobOption.Parameters.Update("CurrencyId", currencyId.ToString());
            jobOption.Parameters = jobOption.Parameters.Update("AccountDate", AccountDate);
            jobOption.Parameters = jobOption.Parameters.Update("CustId", custId.ToString());

            return ExecuteAsync($"ReRunWorldCupDepositDailySummaryJob", jobOption, context, cancellationToken).Result;
        }
    }
}
