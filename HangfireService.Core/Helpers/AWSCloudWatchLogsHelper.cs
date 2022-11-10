using Amazon;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Models.ThirdParty.Saba;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace HangfireService.Core.Helpers;

public class AWSCloudWatchLogsHelper
{
    private readonly IAmazonCloudWatchLogs _client;
    public AWSCloudWatchLogsHelper(IServiceProvider provider)
    {
        var configuration = provider.GetRequiredService<IConfiguration>();
        var awsKeyConfig = provider.GetRequiredService<IOptions<AWSKeyConfigOption>>();
        var proxyHost = configuration.GetValue<string>("ProxyHost");
        var proxyPort = configuration.GetValue<int>("ProxyPort");

        var config = new AmazonCloudWatchLogsConfig
        {
            RegionEndpoint = RegionEndpoint.APSoutheast1
        };

        if (!string.IsNullOrEmpty(proxyHost))
        {
            config.ProxyHost = proxyHost;
            config.ProxyPort = proxyPort;
        }

        _client = new AmazonCloudWatchLogsClient(awsKeyConfig.Value.AccessKey, awsKeyConfig.Value.SecretKey, config);
    }

    public List<AWSSNSCloudWatchResponse> GetCloudWatchLogs(StartQueryRequest request)
    {
        var startQueryResponse = _client.StartQueryAsync(request).Result;
        var getQueryRequest = new GetQueryResultsRequest
        {
            QueryId = startQueryResponse.QueryId
        };
        var getQueryResults = _client.GetQueryResultsAsync(getQueryRequest).Result;
        return getQueryResults.Results.Select(result => JsonConvert.DeserializeObject<AWSSNSCloudWatchResponse>(result[1].Value)).ToList();
    }
}