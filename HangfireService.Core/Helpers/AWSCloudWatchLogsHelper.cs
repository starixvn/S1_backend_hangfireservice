using Amazon;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Models.ThirdParty.Saba;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog.Context;

namespace HangfireService.Core.Helpers;

public class AWSCloudWatchLogsHelper
{
    private readonly IAmazonCloudWatchLogs _client;
    private readonly ILogger<AWSCloudWatchLogsHelper> _logger;
    public AWSCloudWatchLogsHelper(IServiceProvider provider)
    {
        var configuration = provider.GetRequiredService<IConfiguration>();
        var awsKeyConfig = provider.GetRequiredService<IOptions<AWSKeyConfigOption>>();
        var proxyHost = configuration.GetValue<string>("ProxyHost");
        var proxyPort = configuration.GetValue<int>("ProxyPort");
        _logger = provider.GetRequiredService<ILogger<AWSCloudWatchLogsHelper>>();

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

    public string GetCloudWatchLogsQueryId(StartQueryRequest request)
    {
        var startQueryResponse = _client.StartQueryAsync(request).Result;
        return startQueryResponse.QueryId;
    }

    public List<AWSSNSCloudWatchResponse> GetCloudWatchLogsQueryResults(string queryId)
    {
        var getQueryRequest = new GetQueryResultsRequest
        {
            QueryId = queryId
        };
        var getQueryResults = _client.GetQueryResultsAsync(getQueryRequest).Result;
        _logger.LogWarning($"Tp.Request.Response:{JsonConvert.SerializeObject(getQueryResults)}");
        return getQueryResults.Results.Select(result => JsonConvert.DeserializeObject<AWSSNSCloudWatchResponse>(result[1].Value)).ToList();
    }
}