using System.Net;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SecurityToken.Model;
using HangfireService.Core.Extensions;
using HangfireService.Core.Models.AWS;
using HangfireService.Core.Models.Enums;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HangfireService.Core.Helpers;

public class AWSS3Helper
{
    private readonly ILogger<AWSS3Helper> _logger;

    private readonly AWSKeyConfig _awsKeyConfig;
    private readonly AmazonS3Config _amazonS3Config;
    private readonly ICacheHelper _cacheHelper;
    private readonly string _redisKey = "AWSS3Credential";

    public AWSS3Helper(IServiceProvider provider)
    {
        var configBll = provider.GetRequiredService<ConfigBLL>();
        _awsKeyConfig = configBll.GetConfigValue<AWSKeyConfig>("HangfireAwsKey");
        var region = configBll.GetConfigValue("AWSRegion");
        var proxyConfig = configBll.GetConfigValue<ProxyConfig>("ProxySetting");
        var proxyHost = proxyConfig.ProxyHost;
        var proxyPort = proxyConfig.ProxyPort;
        _cacheHelper = provider.GetRequiredService<ICacheHelper>();

        _amazonS3Config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(region)
        };

        if (!string.IsNullOrEmpty(proxyHost))
        {
            _amazonS3Config.ProxyHost = proxyHost;
            _amazonS3Config.ProxyPort = proxyPort;
        }

        _logger = provider.GetRequiredService<ILogger<AWSS3Helper>>();
    }

    public async Task<bool> PutObjectAsync(PutObjectRequest request)
    {
        try
        {
            var client = await GetClientAsync();
            var response = await client.PutObjectAsync(request);
            if (response.HttpStatusCode != HttpStatusCode.OK)
            {
                _logger.LogInformation($"UploadS3File, Bucket={request.BucketName}, Key={request.Key} Response={JsonConvert.SerializeObject(response)}");
            }

            return response.HttpStatusCode == HttpStatusCode.OK;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, MethodExtension.GetCurrentAsyncMethod());
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, MethodExtension.GetCurrentAsyncMethod());
            return false;
        }
    }

    private async Task<AmazonS3Client> GetClientAsync()
    {
        var credentials = new Credentials();

        if (_cacheHelper.KeyExists($"{_redisKey}_1", RedisDataBase.Shared))
            credentials = await _cacheHelper.StringGetAsync<Credentials>($"{_redisKey}_1", RedisDataBase.Shared);

        if (credentials.AccessKeyId is null && _cacheHelper.KeyExists($"{_redisKey}_2", RedisDataBase.Shared))
            credentials = await _cacheHelper.StringGetAsync<Credentials>($"{_redisKey}_2", RedisDataBase.Shared);

        if (credentials.AccessKeyId is not null)
        {
            _logger.LogInformation("Create S3 Clinet By Credentials.");
            return new AmazonS3Client(credentials.AccessKeyId, credentials.SecretAccessKey, credentials.SessionToken, _amazonS3Config);
        }
        else
        {
            _logger.LogInformation("Create S3 Clinet By AwsKey.");
            return new AmazonS3Client(_awsKeyConfig.AccessKey, _awsKeyConfig.SecretKey, _amazonS3Config);
        }
    }
}