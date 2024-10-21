using System.Net;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using HangfireService.Core.Extensions;
using HangfireService.Core.Models.AWS;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OtpNet;

namespace HangfireService.Core.Helpers;

public class AWSS3Helper
{
    private readonly IAmazonS3 _client;
    private readonly ILogger<AWSS3Helper> _logger;

    private readonly AWSKeyConfig _awsKeyConfig;
    private readonly AWSMFAConfig _awsMFAKeyConfig;
    private readonly AmazonS3Config _amazonS3Config;

    public AWSS3Helper(IServiceProvider provider)
    {
        var configBll = provider.GetRequiredService<ConfigBLL>();
        _awsKeyConfig = configBll.GetConfigValue<AWSKeyConfig>("HangfireAwsKey");
        _awsMFAKeyConfig = configBll.GetConfigValue<AWSMFAConfig>("AWSS3WithMFA");
        var region = configBll.GetConfigValue("AWSRegion");
        var proxyConfig = configBll.GetConfigValue<ProxyConfig>("ProxySetting");
        var proxyHost = proxyConfig.ProxyHost;
        var proxyPort = proxyConfig.ProxyPort;

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

        if (_awsMFAKeyConfig != null)
        {
            _logger.LogInformation($"AwsMFAKeyConfig={JsonConvert.SerializeObject(_awsMFAKeyConfig)}");
            var stsClient = new AmazonSecurityTokenServiceClient(_awsMFAKeyConfig.AccessKey, _awsMFAKeyConfig.SecretAccessKey);
            var pass = false;
            var credentials = new Credentials();
            int getTimes = 5;
            while (!pass && getTimes > 0)
            {
                try
                {
                    var tokenCode = new Totp(Base32Encoding.ToBytes(_awsMFAKeyConfig.SecretKey), mode: OtpHashMode.Sha1).ComputeTotp();
                    //_logger.LogInformation($"TokenCode={tokenCode}");
                    var request = new GetSessionTokenRequest
                    {
                        DurationSeconds = _awsMFAKeyConfig.DurationSeconds,
                        SerialNumber = _awsMFAKeyConfig.SerialNumber,
                        TokenCode = tokenCode
                    };
                    //_logger.LogInformation($"GetSessionTokenRequest={JsonConvert.SerializeObject(request)}");
                    var response = await stsClient.GetSessionTokenAsync(request);
                    //_logger.LogInformation($"GetSessionTokenResponse={JsonConvert.SerializeObject(response)}");

                    credentials = response.Credentials;
                    pass = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message, ex);
                    getTimes--;
                }
            }
            return new AmazonS3Client(credentials.AccessKeyId, credentials.SecretAccessKey, credentials.SessionToken, _amazonS3Config);
        }
        else
        {
            return new AmazonS3Client(_awsKeyConfig.AccessKey, _awsKeyConfig.SecretKey, _amazonS3Config);
        }
    }
}