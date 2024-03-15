using HangfireService.Core.DataLayers;
using HangfireService.Core.Helpers;
using HangfireService.Core.Models.Enums;

namespace HangfireService.Core.Service
{
    public class BlockIpService
    {
        private readonly ReportDBDataLayer _reportDBDataLayer;
        private readonly ICacheHelper _redisHelper;
        private readonly string _blockIpPartten = "BlockIp";

        public BlockIpService(IServiceProvider provider)
        {
            _reportDBDataLayer = provider.GetRequiredService<ReportDBDataLayer>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
        }

        public async Task RunJobAsync()
        {
            var dbBlockIpList = await GetDbBlockIp();
            var redisBlockIpList = GetRedisBlockIp();

            AddRedis(dbBlockIpList.Except(redisBlockIpList));
            RemoveRedis(redisBlockIpList.Except(dbBlockIpList));

            await Task.CompletedTask;
        }

        private async Task<IEnumerable<string>> GetDbBlockIp() => await _reportDBDataLayer.GetBlockIp();

        private IEnumerable<string> GetRedisBlockIp()
        {
            var redisBlockIpKeys = _redisHelper.StringGetKeyPersist($"{_blockIpPartten}:*", RedisDataBase.IP);
            return redisBlockIpKeys?.Select(s => s.Split(':').Last()) ?? Enumerable.Empty<string>();
        }

        private void AddRedis(IEnumerable<string> ips)
        {
            foreach (var ip in ips)
                _redisHelper.StringSet($"{_blockIpPartten}:{ip}", string.Empty, null, RedisDataBase.IP);
        }

        private async void RemoveRedis(IEnumerable<string> ips)
        {
            foreach (var ip in ips)
                await _redisHelper.KeyDeleteAsync($"{_blockIpPartten}:{ip}", RedisDataBase.IP);
        }
    }
}
