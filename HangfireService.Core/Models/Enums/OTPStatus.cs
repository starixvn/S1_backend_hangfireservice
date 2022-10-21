namespace HangfireService.Core.Models.Enums
{
    public enum OTPStatus
    {
        SendToProvider = 1,     //呼叫第三方API成功
        SendToCustomer = 2,     //第三方發送至客戶成功
        CustomerVerified = 3,   //客戶完成驗證
        Fail = 4,
    }
}
