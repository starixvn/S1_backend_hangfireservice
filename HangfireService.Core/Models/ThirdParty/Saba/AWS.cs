namespace HangfireService.Core.Models.ThirdParty.Saba;

public class AWSSNSCloudWatchResponse
{
    public AWSSNSCloudWatchNotification notification { get; set; }
    public AWSSNSCloudWatchProviderResponse delivery { get; set; }
    public string status { get; set; }
}

public class AWSSNSCloudWatchNotification
{
    public string messageId { get; set; }
}

public class AWSSNSCloudWatchProviderResponse
{
    public string providerResponse { get; set; }
}