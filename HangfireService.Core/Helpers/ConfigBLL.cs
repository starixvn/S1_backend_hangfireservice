using HangfireService.Core.Models.SiteConfig;

using Microsoft.AspNetCore.Hosting;

using Newtonsoft.Json;

namespace HangfireService.Core.Helpers
{
    public class ConfigBLL
    {
        private readonly Dictionary<string, string> _defaultSiteConfig;

        private readonly Dictionary<int, SiteConfig> _siteIdConfigs;

        private readonly IEnumerable<Config> _rawConfigs;

        private readonly string _env;

        public ConfigBLL(IServiceProvider provider)
        {
            var siteConfigList = provider.GetServices<SiteConfig>();
            _defaultSiteConfig = siteConfigList.First(i => i.SiteId == 0).Config;
            var siteConfigs = siteConfigList.Where(i => i.SiteId != 0);
            _siteIdConfigs = siteConfigs.ToDictionary(i => i.SiteId, i => i);
            _rawConfigs = provider.GetRequiredService<IEnumerable<Config>>();
            var environment = provider.GetRequiredService<IWebHostEnvironment>();
            _env = environment.EnvironmentName;
        }

        public Dictionary<int, TItem> GetConfigValuesBySiteId<TItem>(string configKey)
        {
            var defaultItem = _defaultSiteConfig.TryGetValue(configKey, out var json)
                ? JsonConvert.DeserializeObject<TItem>(json) : default;

            var result = new Dictionary<int, TItem>();
            foreach (var siteConfig in _siteIdConfigs)
            {
                var data = siteConfig.Value.Config.TryGetValue(configKey, out var configValue)
                    ? JsonConvert.DeserializeObject<TItem>(configValue) : defaultItem;
                result.Add(siteConfig.Key, data);
            }
            return result;
        }

        public TItem GetConfigValue<TItem>(string configKey, int siteId = 0)
        {
            var defaultItem = _defaultSiteConfig.TryGetValue(configKey, out var json)
                ? JsonConvert.DeserializeObject<TItem>(json) : default;

            if (siteId == 0)
            {
                return defaultItem;
            }

            if (_siteIdConfigs.TryGetValue(siteId, out var siteConfig))
            {
                return siteConfig.Config.TryGetValue(configKey, out var configValue)
                   ? JsonConvert.DeserializeObject<TItem>(configValue) : defaultItem;
            }

            return defaultItem;
        }

        public T GetConfigValueByEnvironment<T>(string configKey, int siteId = 0)
        {
            var json = GetConfigValue<T>($"{configKey}.{_env}.json", siteId);
            return json;
        }

        public IEnumerable<TItem> GetRawConfigValues<TItem>(string configKey)
        {
            var result = new List<TItem>();
            foreach (var config in _rawConfigs.Where(i => i.ConfigKey == configKey))
            {
                var item = JsonConvert.DeserializeObject<TItem>(config.ConfigValue);
                result.Add(item);
            }
            return result;
        }
    }
}
