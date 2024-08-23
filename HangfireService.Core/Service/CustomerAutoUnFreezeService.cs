using HangfireService.Core.DataLayers;
using HangfireService.Core.Models.Enums;
namespace HangfireService.Core.Service;

public class CustomerAutoUnFreezeService
{
    private readonly MainDBDataLayer _mainDbDataLayer;
    private readonly ReportDBDataLayer _reportDbDataLayer;
    private readonly CustomerService _customerService;
    private const string Operator = "System";
    private const string Message = "Change Customer Status Running, Reason : System auto UnFreeze ";

    public CustomerAutoUnFreezeService(IServiceProvider provider)
    {
        _mainDbDataLayer = provider.GetRequiredService<MainDBDataLayer>();
        _reportDbDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
        _customerService = provider.GetRequiredService<CustomerService>();
    }

    public async Task RunJobAsync()
    {
        var currentTime = DateTime.UtcNow;
        var custTemporaryList = await _reportDbDataLayer.GetCustTemporaryStatus();
        
        foreach (var tempData in custTemporaryList)
        {
            if (currentTime > tempData.ExpiredTime &&
                (tempData.Status is (int) CustomerStatusEnum.FreezeOneWeek or (int) CustomerStatusEnum.FreezeOneMonth or (int) CustomerStatusEnum.FreezeThreeMonth))
            {
                var result = await _mainDbDataLayer.SetCustStatus(tempData.CustId, (int)CustomerStatusEnum.Running , (int)CustTaskType.LastStatusUpdateTime);
                if (result) await _mainDbDataLayer.SetCustNotes(tempData.CustId, Message , Operator);

                await _customerService.RemoveCustTagAsync(tempData.CustId, 35);
            }
        }
        await Task.CompletedTask;
    }
    
}
