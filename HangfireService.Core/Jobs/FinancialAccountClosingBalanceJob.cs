using System.ComponentModel;
using Hangfire;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Server;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Service;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace HangfireService.Core.Jobs;

[ManagementPage("FinancialAccountClosingBalanceJob", queue: "financial")]
public class FinancialAccountClosingBalanceJob: BaseJob, IScheduledJob
{
    private readonly FincanceService _financeService;

    public FinancialAccountClosingBalanceJob(IServiceProvider provider) : base(provider)
    {
        LogContext.PushProperty("ClassName", GetType().Name);
        LogContext.PushProperty("ExecuteId", Guid.NewGuid().ToString());
        _financeService = provider.GetRequiredService<FincanceService>();
    }
    
    [DisplayName("{0}")]
    public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
    {
        _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} Start...");
        var failedCnt = 0;
        try
        {
            // step 1: 撈帳戶
            var accounts = (await _financeService.GetBankFinancialTransferAccountList())
                .OrderBy(x => x.SiteId)
                .ThenBy(x => x.CurrencyId);
            
            // step 2: 更新帳戶關帳金額
            var yyyymm = DateTimeOffset.UtcNow.AddMonths(-1).ToString("yyyyMM");
            foreach (var account in accounts)
            {
                var accInfoStr = $"{account.SiteName}({account.SiteId}), " + 
                                 $"{account.IsoCode}({account.CurrencyId}), {account.DisplayName}({account.Id})"; 
                try
                {
                    var result = _financeService.SetFinancialAccountClosingBalance(account.Id, yyyymm);
                    _logger.LogInformation(accInfoStr + ", Result: " + GetSetClosingBalanceErrorMsg(result));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, accInfoStr + ", Exception");
                    failedCnt++;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message, $"{DateTime.Now:HH:mm:ss} || {jobName} Exception.");
            return false;
        }

        
        _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} End, Failed Count: {failedCnt}.");
        return true;
    }

    private static string GetSetClosingBalanceErrorMsg(int result)
    {
        return !SetClosingBalanceErrorMsgMap.ContainsKey(result)
            ? string.Empty
            : SetClosingBalanceErrorMsgMap[result];
    }
    
    private static Dictionary<int, string> SetClosingBalanceErrorMsgMap =>
    new ()
    {
        { 0, "Success." },
        { -1, "Account data not found." },
        { -2, "Closing balance has been set." },
        { -3, "Failed to set closing balance." },
        { -9, "Unknown error." }
    };
}