using System.ComponentModel;
using Hangfire;
using Hangfire.Server;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Customer;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Service;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;

namespace HangfireService.Core.Jobs;
public class TaggingDuplicateJob : BaseJob, IScheduledJob
{
    private readonly CustomerService _customerService;
    private readonly IEnumerable<ServerInfo> _serverInfos;

    public TaggingDuplicateJob(IServiceProvider provider) : base(provider)
    {
        LogContext.PushProperty("ClassName", GetType().Name);
        LogContext.PushProperty("ExecuteId", Guid.NewGuid().ToString());
        _customerService = provider.GetRequiredService<CustomerService>();
        _serverInfos = provider.GetRequiredService<IEnumerable<ServerInfo>>();
    }

    [DisplayName("{0}")]
    public async Task<object> ExecuteAsync(string jobName, JobOption jobOption, PerformContext context, IJobCancellationToken cancellationToken)
    {
        _logger.LogInformation($"{DateTime.Now:HH:mm:ss} || {jobName} Start...");
        var failedCnt = 0;

        try
        {
            var siteIds = _serverInfos.Select(r => r.SiteId);
            foreach (var siteId in siteIds)
            {
                // step 1: 撈帳戶名
                var accountNameList = await _customerService.GetAllAccountNameList(siteId);

                // step 2: 比對
                var duplicateCustList = new List<int>();
                foreach (var account in accountNameList)
                {
                    var patterns = new[]
                    {
                        account.AccountName.Trim(),
                        account.AccountName.Replace(" ", string.Empty)
                    };

                    var isDuplicate = accountNameList.Any(x => x.CustId != account.CustId
                        && !string.IsNullOrWhiteSpace(x.AccountName)
                        && patterns.Any(y => y.Equals(x.AccountName.Trim(), StringComparison.CurrentCultureIgnoreCase)));

                    if (isDuplicate)
                    {
                        duplicateCustList.Add(account.CustId);
                    }
                }

                // step 3: 更新 TagList
                var idx = 0;
                const int cnt = 1000;
                while (idx * cnt < duplicateCustList.Count)
                {
                    var tagList = new List<CustTagList>();
                    var custList = duplicateCustList.Skip(idx * cnt)
                        .Take(cnt)
                        .ToList();

                    try
                    {
                        // 取出用戶的 TagList
                        var custTagList = await _customerService.GetCustTagList(custList);
                        foreach (var custTag in custTagList)
                        {
                            var json = JsonConvert.DeserializeObject<List<int>>(custTag.StrTagList ?? "[]");
                            if (!json.Contains((int)TagEnum.Duplicate))
                            {
                                json.Add((int)TagEnum.Duplicate);
                                tagList.Add(new CustTagList
                                {
                                    CustId = custTag.CustId,
                                    TagList = json
                                });
                            }
                        }

                        // 壓 tag
                        var result = await _customerService.SetCustInfoTagList(tagList);

                        if (result != tagList.Count)
                        {
                            _logger.LogInformation($"{tagList.Count} data should be update, but only {result}.");
                        }

                        _logger.LogInformation($"Processing: {idx * cnt + custList.Count}/{duplicateCustList.Count}, tagging user: {result}");
                    }
                    catch (Exception ex)
                    {
                        failedCnt += custList.Count;
                        _logger.LogError(ex, ex.Message,
                            $"{DateTime.Now:HH:mm:ss} || Exception when update tag, customers: {JsonConvert.SerializeObject(custList)}");
                    }

                    idx++;
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

    private enum TagEnum
    {
        Duplicate = 6
    }
}