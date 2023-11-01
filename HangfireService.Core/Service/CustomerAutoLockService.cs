using System.Text.Json;

using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;

using Microsoft.Extensions.Logging;

namespace HangfireService.Core.Service;

public class CustomerAutoLockService
{
    private readonly MainDBDataLayer _mainDBDataLayer;
    private readonly ReportDBDataLayer _reportDBDataLayer;
    private readonly ILogger<BaseJob> _logger;
    private readonly string _operator = "System";
    private readonly string _message = "None any deposit/valid stake {0} days (follow by deposit approved time and last place bet time)";

    public CustomerAutoLockService(IServiceProvider provider)
    {
        _mainDBDataLayer = provider.GetRequiredService<MainDBDataLayer>();
        _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
        _logger = provider.GetRequiredService<ILogger<BaseJob>>();
    }

    public async Task RunJobAsync()
    {
        var currentTime = DateTime.UtcNow;
        var dormantSettings = await GetDormantSetting();
        foreach (var dormantSetting in dormantSettings)
        {
            if ((currentTime - dormantSetting.LastUpdateTime).TotalDays < 1) continue;

            foreach (var days in dormantSetting.Days)
            {
                var unDepositCustomers = await GetDormantCustomer(dormantSetting.SiteId, days,
                    new List<CustomerStatusEnum> { CustomerStatusEnum.Running });

                foreach (var unDepositCustomer in unDepositCustomers)
                {
                    if ((currentTime - unDepositCustomer.LastLockTime).TotalDays > 180)
                    {
                        var result = await SetCustomerStatusToLock(unDepositCustomer.CustId);
                        if (result >= 0)
                            await SetCustNotes(unDepositCustomer.CustId, string.Format(_message, days.ToString()), _operator);
                    }
                }
            }
            await SetDormantSetting(dormantSetting.SiteId);
        }

        await Task.CompletedTask;
    }

    private async Task<IEnumerable<DormantSettingDto>> GetDormantSetting()
        => await _reportDBDataLayer.GetDormantSetting();

    private async Task<IEnumerable<DormantCustomerDto>> GetDormantCustomer(int siteId, int days, List<CustomerStatusEnum> statusList)
        => await _reportDBDataLayer.GetDormantCustomer(siteId, days, JsonSerializer.Serialize(statusList.Select(x => (int)x)));

    private async Task<int> SetCustomerStatusToLock(int custId)
        => await _mainDBDataLayer.SetCustomerStatus(custId, (int)CustomerStatusEnum.Lock);

    private async Task<int> SetCustNotes(int custId, string note, string @operator)
        => await _mainDBDataLayer.SetCustNotes(custId, note, @operator);

    private async Task<int> SetDormantSetting(int siteId)
        => await _mainDBDataLayer.SetDormantSetting(siteId);
}
