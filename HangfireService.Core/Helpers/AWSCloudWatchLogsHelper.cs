using Amazon;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using HangfireService.Core.Models;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Models.ThirdParty.Saba;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HangfireService.Core.Helpers;

public class AWSCloudWatchLogsHelper
{
    private readonly IAmazonCloudWatchLogs _client;
    private readonly ILogger<AWSCloudWatchLogsHelper> _logger;
    public AWSCloudWatchLogsHelper(IServiceProvider provider)
    {
        var configBll = provider.GetRequiredService<ConfigBLL>();
        

        var configuration = provider.GetRequiredService<IConfiguration>();
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

        var awsKeyConfig = configBll.GetConfigValuesBySiteId<AwsCloudWatchConfig>("AwsCloudWatchConfig").First().Value;
        _client = new AmazonCloudWatchLogsClient(awsKeyConfig.AccessKey, awsKeyConfig.SecretKey, config);
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