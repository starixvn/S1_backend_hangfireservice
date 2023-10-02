using HangfireService.Core.Models.SiteConfig;
using Newtonsoft.Json;

namespace HangfireService.Core.Helpers
{
    public class ConfigBLL
    {
        private readonly Dictionary<string, string> _defaultSiteConfig;
        private readonly Dictionary<string, SiteConfig> _siteNameConfigs;
        private readonly Dictionary<int, SiteConfig> _siteIdConfigs;

        public ConfigBLL(IServiceProvider provider)
        {
            var siteConfigList = provider.GetServices<SiteConfig>();
            _defaultSiteConfig = siteConfigList.First(i => i.SiteId == 0).Config;
            var siteConfigs = siteConfigList.Where(i => i.SiteId != 0);
            _siteNameConfigs = new Dictionary<string, SiteConfig>(siteConfigs.ToDictionary(i => i.SiteName, i => i), StringComparer.OrdinalIgnoreCase);
            _siteIdConfigs = siteConfigs.ToDictionary(i => i.SiteId, i => i);
        }

        public string GetConfigValue(string siteName, string configKey)
        {
            var defaultValue = _defaultSiteConfig.GetValueOrDefault(configKey, string.Empty);
            if (_siteNameConfigs.TryGetValue(siteName, out var siteConfig))
            {
                return siteConfig.Config.TryGetValue(configKey, out var configValue) ? configValue : defaultValue;
            }

            return defaultValue;
        }

        public string GetConfigValue(int siteId, string configKey)
        {
            var defaultValue = _defaultSiteConfig.GetValueOrDefault(configKey, string.Empty);
            if (_siteIdConfigs.TryGetValue(siteId, out var siteConfig))
            {
                return siteConfig.Config.TryGetValue(configKey, out var configValue) ? configValue : defaultValue;
            }

            return defaultValue;
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

        public Dictionary<string, TItem> GetConfigValuesBySiteName<TItem>(string configKey)
        {
            var defaultItem = _defaultSiteConfig.TryGetValue(configKey, out var json)
               ? JsonConvert.DeserializeObject<TItem>(json) : default;

            var result = new Dictionary<string, TItem>(StringComparer.OrdinalIgnoreCase);
            foreach (var siteConfig in _siteNameConfigs)
            {
                var data = siteConfig.Value.Config.TryGetValue(configKey, out var configValue)
                   ? JsonConvert.DeserializeObject<TItem>(configValue) : defaultItem;
                result.Add(siteConfig.Key, data);
            }
            return result;
        }
    }
}
