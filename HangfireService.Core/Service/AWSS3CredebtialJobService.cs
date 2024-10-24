using Amazon.SecurityToken.Model;
using Amazon.SecurityToken;
using HangfireService.Core.Helpers;
using HangfireService.Core.Models.Enums;
using Newtonsoft.Json;
using OtpNet;
using HangfireService.Core.Jobs;
using Microsoft.Extensions.Logging;
using HangfireService.Core.Models.AWS;

namespace HangfireService.Core.Service;

public class AWSS3CredebtialJobService
{
    private readonly AWSMFAConfig _awsMFAKeyConfig;
    private readonly ICacheHelper _redisHelper;
    private readonly ILogger<BaseJob> _logger;

    private readonly string _redisKeyPerfix = "AWSS3Credential";

    public AWSS3CredebtialJobService(IServiceProvider provider)
    {
        //取得s3 config
        var configBll = provider.GetRequiredService<ConfigBLL>();
        _awsMFAKeyConfig = configBll.GetConfigValue<AWSMFAConfig>("AWSS3WithMFA");

        _redisHelper = provider.GetRequiredService<ICacheHelper>();
        _logger = provider.GetRequiredService<ILogger<BaseJob>>();
    }

    public async Task RunCheckJobAsync(int keyId)
    {
        var redisCredentials = GetFromRedis(keyId);

        if (redisCredentials.Expiration <= DateTime.Now.AddHours(-1))
            await RunUpdateJobAsync(keyId);

        await Task.CompletedTask;
    }

    public async Task RunUpdateJobAsync(int keyId)
    {
        var newCredential = await GetCredentialsAsync();
        AddRedis(keyId, JsonConvert.SerializeObject(newCredential));

        await Task.CompletedTask;
    }

    private Credentials GetFromRedis(int keyId)
    {
        var value = _redisHelper.StringGet($"{_redisKeyPerfix}_{keyId}", RedisDataBase.Shared);
        return string.IsNullOrWhiteSpace(value) ? new Credentials() : JsonConvert.DeserializeObject<Credentials>(value);
    }

    private void AddRedis(int keyId, string value)
    {
        _redisHelper.StringSet($"{_redisKeyPerfix}_{keyId}", value, null, RedisDataBase.Shared);
    }

    private async Task<Credentials> GetCredentialsAsync()
    {
        _logger.LogInformation($"AwsMFAKeyConfig={JsonConvert.SerializeObject(_awsMFAKeyConfig)}");
        var stsClient = new AmazonSecurityTokenServiceClient(_awsMFAKeyConfig.AccessKey, _awsMFAKeyConfig.SecretAccessKey);
        var pass = false;
        var credentials = new Credentials();
        int getTimes = 10;
        while (!pass && getTimes > 0)
        {
            try
            {
                var tokenCode = new Totp(Base32Encoding.ToBytes(_awsMFAKeyConfig.SecretKey), mode: OtpHashMode.Sha1).ComputeTotp();
                _logger.LogInformation($"TokenCode={tokenCode}");
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

                Thread.Sleep(7000); //5秒後重新嘗試
            }
        }
        return credentials;
    }
}
