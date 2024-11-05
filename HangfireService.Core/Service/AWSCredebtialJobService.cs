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

public class AWSCredebtialJobService
{
    private AWSMFAConfig _awsMFAKeyConfig;
    private readonly ConfigBLL _configBLL;
    private readonly ICacheHelper _redisHelper;
    private readonly ILogger<BaseJob> _logger;

    public AWSCredebtialJobService(IServiceProvider provider)
    {
        //取得s3 config
        _configBLL = provider.GetRequiredService<ConfigBLL>();
        _redisHelper = provider.GetRequiredService<ICacheHelper>();
        _logger = provider.GetRequiredService<ILogger<BaseJob>>();
    }

    public async Task RunCheckJobAsync(string keyName, int keyId)
    {
        var redisCredentials = GetFromRedis(keyName, keyId);

        if (redisCredentials.Expiration <= DateTime.UtcNow.AddHours(1))
            await RunUpdateJobAsync(keyName, keyId);

        await Task.CompletedTask;
    }

    public async Task RunUpdateJobAsync(string keyName, int keyId)
    {
        var newCredential = await GetCredentialsAsync(keyName);
        if (newCredential is not null)
            AddRedis(keyName, keyId, JsonConvert.SerializeObject(newCredential));

        await Task.CompletedTask;
    }

    private Credentials GetFromRedis(string keyName, int keyId)
    {
        var value = _redisHelper.StringGet($"{keyName}_{keyId}", RedisDataBase.Shared);
        return string.IsNullOrWhiteSpace(value) ? new Credentials() : JsonConvert.DeserializeObject<Credentials>(value);
    }

    private void AddRedis(string keyName, int keyId, string value)
    {
        _redisHelper.StringSet($"{keyName}_{keyId}", value, null, RedisDataBase.Shared);
    }

    private async Task<Credentials> GetCredentialsAsync(string configKey)
    {
        _awsMFAKeyConfig = _configBLL.GetConfigValue<AWSMFAConfig>(configKey);
        _logger.LogInformation($"Get SiteConfigKey:{configKey}={JsonConvert.SerializeObject(_awsMFAKeyConfig)}");

        if (_awsMFAKeyConfig is not null)
        {
            var credentials = new Credentials();
            var stsClient = new AmazonSecurityTokenServiceClient(_awsMFAKeyConfig.AccessKey, _awsMFAKeyConfig.SecretAccessKey);
            var pass = false;
            int getTimes = 10;  //max try times
            while (!pass && getTimes > 0)
            {
                try
                {
                    var tokenCode = new Totp(Base32Encoding.ToBytes(_awsMFAKeyConfig.SecretKey), mode: OtpHashMode.Sha1).ComputeTotp();
                    _logger.LogInformation($"TokenCode={tokenCode}");
                    var request = new GetSessionTokenRequest
                    {
                        DurationSeconds = _awsMFAKeyConfig.DurationSeconds, //憑證到期時間
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

                    Thread.Sleep(7000); //7秒後重新嘗試
                }
            }
            return credentials;
        }
        else
        {
            return null;
        }
    }
}
