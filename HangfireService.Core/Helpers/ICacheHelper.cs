
using HangfireService.Core.Models.Enums;

using StackExchange.Redis;

namespace HangfireService.Core.Helpers
{
    public interface ICacheHelper
    {
        TItem TryGetOrCreate<TItem>(string key, TimeSpan timeSpan, Func<TItem> factory,
            RedisDataBase redisDataBase = RedisDataBase.Member);

        Task<TItem> TryGetOrCreateAsync<TItem>(string key, TimeSpan? timeSpan, Func<Task<TItem>> factory,
            RedisDataBase redisDataBase = RedisDataBase.Member);

        bool StringSet(string key, RedisValue value, TimeSpan? timeSpan,
            RedisDataBase redisDataBase = RedisDataBase.Member);

        string StringGet(string key, RedisDataBase redisDataBase = RedisDataBase.Member);

        bool KeyExists(string key, RedisDataBase redisDataBase = RedisDataBase.Member);

        Task<bool> KeyDeleteAsync(string key, RedisDataBase redisDataBase = RedisDataBase.Member);
    }

}
