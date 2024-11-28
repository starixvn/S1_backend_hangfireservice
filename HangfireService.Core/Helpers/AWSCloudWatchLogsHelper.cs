using Amazon;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;

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

    public AWSCloudWatchLogsHelper(IServiceProvider provider, IConfiguration configuration)
    {
        _logger = provider.GetRequiredService<ILogger<AWSCloudWatchLogsHelper>>();

        var configBll = provider.GetRequiredService<ConfigBLL>();

        var config = new AmazonCloudWatchLogsConfig
        {
            RegionEndpoint = RegionEndpoint.APSoutheast1
        };

        if (!string.IsNullOrWhiteSpace(configuration.GetValue<string>("ProxyHost")))
        {
            config.ProxyHost = configuration.GetValue<string>("ProxyHost");
            config.ProxyPort = configuration.GetValue<int>("ProxyPort");
        }

        var awsCloudWatchConfig = configBll.GetConfigValue<AwsCloudWatchConfig>("AwsCloudWatch");
        _client = new AmazonCloudWatchLogsClient(awsCloudWatchConfig.AccessKey, awsCloudWatchConfig.SecretKey, config);
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