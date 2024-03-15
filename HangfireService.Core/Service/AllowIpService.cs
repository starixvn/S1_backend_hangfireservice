using HangfireService.Core.DataLayers;
using HangfireService.Core.Helpers;
using HangfireService.Core.Models.Enums;

namespace HangfireService.Core.Service
{
    public class AllowIpService
    {
        private readonly ReportDBDataLayer _reportDBDataLayer;
        private readonly ICacheHelper _redisHelper;
        private readonly string _allowedIpPartten = "AllowedIp";

        public AllowIpService(IServiceProvider provider)
        {
            _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
        }

        public async Task RunJobAsync()
        {
            var dbAllowedIpList = await GetDbAllowIp();
            var redisAllowedIpList = GetRedisAllowIp();

            AddRedis(dbAllowedIpList.Except(redisAllowedIpList));
            RemoveRedis(redisAllowedIpList.Except(dbAllowedIpList));

            await Task.CompletedTask;
        }

        private async Task<IEnumerable<string>> GetDbAllowIp() => await _reportDBDataLayer.GetAllowIp();

        private IEnumerable<string> GetRedisAllowIp()
        {
            var redisAllowedIpKeys = _redisHelper.StringGetKeyPersist($"{_allowedIpPartten}:*", RedisDataBase.IP);
            return redisAllowedIpKeys?.Select(s => s.Split(':').Last()) ?? Enumerable.Empty<string>();
        }

        private void AddRedis(IEnumerable<string> ips)
        {
            foreach (var ip in ips)
                _redisHelper.StringSet($"{_allowedIpPartten}:{ip}", string.Empty, null, RedisDataBase.IP);
        }

        private async void RemoveRedis(IEnumerable<string> ips)
        {
            foreach (var ip in ips)
                await _redisHelper.KeyDeleteAsync($"{_allowedIpPartten}:{ip}", RedisDataBase.IP);
        }
    }
}
