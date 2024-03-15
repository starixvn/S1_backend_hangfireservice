using HangfireService.Core.Models.Enums;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using StackExchange.Redis;

namespace HangfireService.Core.Helpers
{
    public class RedisCacheHelper : ICacheHelper
    {
        private readonly IConnectionMultiplexer _multiplexer;
        private readonly ILogger<RedisCacheHelper> _logger;
        private readonly string[] _charsToRemove = { "[", "]", "/", " ", "," };

        public RedisCacheHelper(IConnectionMultiplexer multiplexer, ILogger<RedisCacheHelper> logger)
        {
            _multiplexer = multiplexer;
            _logger = logger;
        }

        public TItem TryGetOrCreate<TItem>(string key, TimeSpan timeSpan, Func<TItem> factory, RedisDataBase redisDataBase = RedisDataBase.Member)
        {
            key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
            try
            {
                var db = _multiplexer.GetDatabase((int)redisDataBase);

                if (!db.KeyExists(key))
                {
                    var result = factory();
                    if (result != null)
                    {
                        db.StringSet(key, JsonConvert.SerializeObject(result), timeSpan);
                    }

                    return result;
                }

                var json = db.StringGet(key);
                return json.HasValue ? JsonConvert.DeserializeObject<TItem>(json) : factory();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return factory();
            }
        }

        public async Task<TItem> TryGetOrCreateAsync<TItem>(string key, TimeSpan? timeSpan, Func<Task<TItem>> factory, RedisDataBase redisDataBase = RedisDataBase.Member)
        {
            key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
            try
            {
                var db = _multiplexer.GetDatabase((int)redisDataBase);

                if (!db.KeyExists(key))
                {
                    var result = await factory();
                    if (result != null)
                    {
                        await db.StringSetAsync(key, JsonConvert.SerializeObject(result), timeSpan);
                    }

                    return result;
                }

                var json = db.StringGet(key);
                return json.HasValue ? JsonConvert.DeserializeObject<TItem>(json) : await factory();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return await factory();
            }
        }

        public bool StringSet(string key, RedisValue value, TimeSpan? timeSpan, RedisDataBase redisDataBase = RedisDataBase.Member)
        {
            key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
            try
            {
                var db = _multiplexer.GetDatabase((int)redisDataBase);
                return db.StringSet(key, value, timeSpan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return false;
            }
        }

        public string StringGet(string key, RedisDataBase redisDataBase = RedisDataBase.Member)
        {
            key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
            try
            {
                var db = _multiplexer.GetDatabase((int)redisDataBase);
                return db.StringGet(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return default;
            }
        }

        public bool KeyExists(string key, RedisDataBase redisDataBase = RedisDataBase.Member)
        {
            key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
            try
            {
                var db = _multiplexer.GetDatabase((int)redisDataBase);
                return db.KeyExists(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return false;
            }
        }

        public async Task<bool> KeyDeleteAsync(string key, RedisDataBase redisDataBase = RedisDataBase.Member)
        {
            try
            {
                key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
                var db = _multiplexer.GetDatabase((int)redisDataBase);
                return await db.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return default;
            }
        }

        public bool TryCreateLock(RedisKey lockKey, RedisValue lockValue, TimeSpan? expiry, RedisDataBase redisDataBase = RedisDataBase.Member)
        {
            var key = $"{redisDataBase}:{RemoveSpecialChars(lockKey)}";
            try
            {
                var db = _multiplexer.GetDatabase((int)redisDataBase);
                return db.StringSet(key, lockValue, expiry, When.NotExists);// 使用When.NotExists選項，確保操作的原子性
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return false;
            }
        }

        public bool UnLock(RedisKey lockKey, RedisValue lockValue, RedisDataBase redisDataBase = RedisDataBase.Member)
        {
            //當鎖的值與預期的值相匹配時才釋放鎖，使用Lua script確保操作的原子性
            var key = $"{redisDataBase}:{RemoveSpecialChars(lockKey)}";
            var script = @"if redis.call('GET', KEYS[1]) == ARGV[1] then 
                           return redis.call('DEL', KEYS[1]) 
                       else 
                           return 0 
                       end";
            try
            {
                var db = _multiplexer.GetDatabase((int)redisDataBase);
                var result = db.ScriptEvaluate(script, new RedisKey[] { key }, new RedisValue[] { lockValue });
                return result.ToString() == "1";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{key} | {ex.Message}");
                return false;
            }
        }

        private string RemoveSpecialChars(string key)
        {
            foreach (var c in _charsToRemove)
            {
                key = key.Replace(c, string.Empty);
            }

            return key;
        }
    }
}
