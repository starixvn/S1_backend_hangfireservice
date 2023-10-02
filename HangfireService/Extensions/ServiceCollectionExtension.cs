using Hangfire;
using Hangfire.Console.Extensions;
using Hangfire.Dashboard.Management;
using Hangfire.MemoryStorage;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Helpers;
using HangfireService.Core.Jobs;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Service;
using HangfireService.Core.ThirdParty;
using HangfireService.Core.ThirdParty.Interfaces;
using System.Net;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Promotion;
using StackExchange.Redis;
using HangfireService.Core.Models.SiteConfig;

namespace HangfireService.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddDatabaseConnection(this IServiceCollection services, IConfiguration configuration)
        {
            var provider = services.BuildServiceProvider();
            var logger = provider.GetRequiredService<ILogger<MySqlClient>>();
            var serviceType = Environment.GetEnvironmentVariable("ServiceType") ?? configuration.GetValue<string>("ServiceType");
            return services
            .AddSingleton(param => new MySqlClient(configuration.GetConnectionString($"{serviceType}_MainDB"), logger, DataBase.Main))
            .AddSingleton(param => new MySqlClient(configuration.GetConnectionString($"{serviceType}_ReportDB"), logger, DataBase.Report))
            .AddSingleton(param => new MySqlClient(configuration.GetConnectionString($"{serviceType}_WagerDB"), logger, DataBase.Wager));
        }

        public static IServiceCollection AddRedisConnection(this IServiceCollection services, IConfiguration configuration)
        {
            return services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(configuration.GetValue<string>("RedisConnection")))
                            .AddSingleton<ICacheHelper, RedisCacheHelper>();
        }

        public static IServiceCollection AddHangfireService(this IServiceCollection services, IConfiguration configuration)
        {
            var provider = services.BuildServiceProvider();
            var jobGroups = provider.GetRequiredService<Dictionary<string, List<JobOption>>>();

            return services
            .AddHangfire(config =>
                {
                    config
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseColouredConsoleLogProvider()
                    .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.RecurringJobCount)
                    .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.RetriesCount)
                    .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.ProcessingCount)
                    .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.SucceededCount)
                    .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.FailedCount)
                    .UseDashboardMetric(Hangfire.Dashboard.DashboardMetrics.AwaitingCount)
                    .UseManagementPages(p => p.AddJobs(()
                        => typeof(IScheduledJob).Assembly.GetTypes().Where(item => item.GetInterfaces().Contains(typeof(IScheduledJob))).ToArray()))
                    .UseSerilogLogProvider()
                    .UseMemoryStorage();
                })
            .AddHangfireConsoleExtensions()
            .AddHangfireServer(config =>
            {
                config.SchedulePollingInterval = TimeSpan.FromSeconds(15);
                var queues = jobGroups.Select(item => item.Key.ToLower()).ToList();
                if (!queues.Contains("default"))
                {
                    queues.Add("default");
                }
                config.Queues = queues.ToArray();
                config.WorkerCount = Environment.ProcessorCount * 10;
            });
        }

        public static IServiceCollection RegisterServices(this IServiceCollection services, IConfiguration configuration)
        {
            var proxyHost = configuration.GetValue<string?>("ProxyHost");
            if (!string.IsNullOrEmpty(proxyHost))
            {
                var proxy = new HttpClientHandler
                {
                    Proxy = new WebProxy(configuration.GetValue<string>("ProxyHost"), configuration.GetValue<int>("ProxyPort")),
                    UseProxy = true
                };

                services.AddHttpClient("with_proxy")
                .ConfigurePrimaryHttpMessageHandler(() => proxy);
            }
            else
            {
                services.AddHttpClient();
            }

            return services
                .AddSingleton<HttpHelper>()
                .AddSingleton<SecureHelper>()
                .AddSingleton<ExcelHelper>()
                .AddSingleton<TelegramBotHelper>()
                .AddSingleton<AWSCloudWatchLogsHelper>()
                .AddTransient<MainDBDataLayer>()
                .AddTransient<WagerDBDataLayer>()
                .AddTransient<ReportDBDataLayer>()
                .AddSingleton<ThirdPartyManager>()
                .AddSingleton<PartitionService>()
                .AddSingleton<FincanceService>()
                .AddSingleton<CustomerService>()
                .AddSingleton<WorldCupDepositDailySummaryService>()
                .AddSingleton<OnlineCountService>()
                .AddSingleton<CustomerAutoLockService>()
                .AddSingleton<CustClosingBalanceService>()
                .AddSingleton<MpaUpdateExchangeRateService>()
                .AddSingleton<AffiliateApi>()
                .AddSingleton<UpdateAffiliateInfoService>()
                .AddSingleton<ConfigService>()
                .AddSingleton<ConfigBLL>();
        }

        public static IServiceCollection RegisterPromotion(this IServiceCollection services)
        {
            services
                .AddSingleton<IBonusSummaryTimerService, BonusSummaryTimerService>()
                .AddSingleton<IPromotionService, PromotionService>();

            var provider = services.BuildServiceProvider();
            var timerService = provider.GetRequiredService<IBonusSummaryTimerService>();
            var promotionService = provider.GetRequiredService<IPromotionService>();
            var rLogger = provider.GetRequiredService<ILogger<ReferralPromotion>>();
            var rfLogger = provider.GetRequiredService<ILogger<ReferralFreeCreditPromotion>>();
            var vLogger = provider.GetRequiredService<ILogger<VipUpgradePromotion>>();
            var oLogger = provider.GetRequiredService<ILogger<OtherPromotion>>();

            services
                .AddSingleton<PromotionSummaryBase>(
                    _ => new ReferralPromotion(timerService, promotionService, WorkerType.Referral, rLogger))
                .AddSingleton<PromotionSummaryBase>(
                    _ => new ReferralFreeCreditPromotion(timerService, promotionService, WorkerType.ReferralFreeCredit, rfLogger))
                .AddSingleton<PromotionSummaryBase>(
                    _ => new VipUpgradePromotion(timerService, promotionService, WorkerType.VipUpgrade, vLogger))
                .AddSingleton<PromotionSummaryBase>(
                    _ => new OtherPromotion(timerService, promotionService, WorkerType.Other, oLogger));
            return services;
        }

        public static IServiceCollection RegisterScheduledJobs(this IServiceCollection services, IConfiguration configuration)
        {
            var provider = services.BuildServiceProvider();
            var configService = provider.GetRequiredService<ConfigService>();
            var jobGroups = configService.Query<JobOption>("Service_ScheduledJob_Get", new { Project = "Hangfire" })
               .GroupBy(i => i.Group)
               .ToDictionary(i => i.Key, i => i.ToList());
            services.AddSingleton(provider => jobGroups);
            MyJobManager.SettingJob(jobGroups);

            var scheduledJobs = typeof(IScheduledJob).Assembly.GetTypes().Where(item => item.GetInterfaces().Contains(typeof(IScheduledJob)));
            foreach (var job in scheduledJobs)
            {
                services.AddSingleton(provider => (IScheduledJob)Activator.CreateInstance(job, new object[] { provider }));
            }

            return services;
        }

        public static IServiceCollection RegisterTpHelper(this IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();
            services.AddSingleton<BaseTpHelper>();
            var tpList = provider.GetRequiredService<ThirdPartyManager>().GetAllTpList();
            services.AddSingleton(provider => tpList);

            var detailTypes = typeof(IDetailTpHelper).Assembly.GetTypes().Where(item => item.GetInterfaces().Contains(typeof(IDetailTpHelper)));
            foreach (var tp in detailTypes)
            {
                services.AddSingleton(provider => (IDetailTpHelper)Activator.CreateInstance(tp, new object[] { provider }));
            }

            var financeTypes = typeof(IFinanceTpHelper).Assembly.GetTypes().Where(item => item.GetInterfaces().Contains(typeof(IFinanceTpHelper)));
            foreach (var tp in financeTypes)
            {
                services.AddSingleton(provider => (IFinanceTpHelper)Activator.CreateInstance(tp, new object[] { provider }));
            }

            return services;
        }

        private static IServiceCollection AddOptionListByDB<T>(this IServiceCollection services, string sql, object parameters = null)
        {
            var provider = services.BuildServiceProvider();
            var configService = provider.GetRequiredService<ConfigService>();
            var option = configService.Query<T>(sql, parameters);
            services.AddSingleton(typeof(IEnumerable<T>), p => option);
            return services;
        }

        public static IServiceCollection AddOption(this IServiceCollection services)
        {
            return services.AddOptionListByDB<CurrencyOption>("Common_Currency_Get");
        }

        public static IServiceCollection AddConfig(this IServiceCollection services, string configGroup)
        {
            return services.AddOptionListByDB<HangfireService.Core.Models.SiteConfig.Config>("SiteConfig_Get", new { ConfigGroup = configGroup });
        }

        public static IServiceCollection AddServerInfo(this IServiceCollection services)
        {
            return services.AddOptionListByDB<ServerInfo>("Admin_ServerInfoList_Get");
        }

        public static IServiceCollection AddSiteConfig(this IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();
            // 準備ServerInfo
            var serverInfoList = new List<ServerInfo>() { new ServerInfo() { SiteId = 0, SiteName = "default" } };
            serverInfoList.AddRange(provider.GetRequiredService<IEnumerable<ServerInfo>>());

            // 準備Config
            var siteConfigGroup = provider.GetRequiredService<IEnumerable<HangfireService.Core.Models.SiteConfig.Config>>().GroupBy(i => i.SiteId);

            foreach (var serverInfo in serverInfoList)
            {
                //設定SiteConfig
                var siteConfig = siteConfigGroup.FirstOrDefault(i => i.Key == serverInfo.SiteId) ?? siteConfigGroup.First(i => i.Key == 0);
                var config = siteConfig.ToDictionary(i => i.ConfigKey, i => i.ConfigValue);
                services.AddSingleton(new SiteConfig(serverInfo, config));
            }
            return services;
        }
    }
}



