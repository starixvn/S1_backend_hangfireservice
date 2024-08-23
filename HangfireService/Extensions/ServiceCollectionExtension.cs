using System.Net;

using Hangfire;
using Hangfire.Console.Extensions;
using Hangfire.Dashboard.Management;
using Hangfire.MemoryStorage;

using HangfireService.Core.DataLayers;
using HangfireService.Core.Helpers;
using HangfireService.Core.Jobs;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.DBs;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.Models.Options;
using HangfireService.Core.Models.SiteConfig;
using HangfireService.Core.Promotion;
using HangfireService.Core.Service;
using HangfireService.Core.ThirdParty;
using HangfireService.Core.ThirdParty.Interfaces;

using StackExchange.Redis;

namespace HangfireService.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddDatabaseConnection(this IServiceCollection services, IConfiguration config)
        {
            var serviceType = Environment.GetEnvironmentVariable("ServiceType") ?? config.GetValue<string>("ServiceType");
            var reportDBConn = config.GetConnectionString($"{serviceType}_ReportDB");
            var reportBackupDB1 = config.GetConnectionString($"{serviceType}_ReportBackupDB1");
            var reportBackupDB2 = config.GetConnectionString($"{serviceType}_ReportBackupDB2");

            //檢查是否有BackupDB連線字串
            if (!string.IsNullOrEmpty(reportBackupDB1) && !string.IsNullOrEmpty(reportBackupDB2))
            {
                // 設定ReportDB連線順序
                services.AddSingleton(provider => new MySqlClient(provider, DataBase.Report, reportDBConn, new List<string>()
                {
                    reportDBConn,   //1b
                    reportBackupDB1,//1c
                    reportBackupDB2,//1a
                }));

                // 設定ReportBackupDB連線順序
                services.AddSingleton(provider => new MySqlClient(provider, DataBase.ReportBackupDB, reportBackupDB1, new List<string>()
                {
                    reportBackupDB1,//1c
                    reportDBConn,   //1b
                    reportBackupDB2,//1a
                }));
            }
            else
            {
                services.AddSingleton(provider => new MySqlClient(provider, DataBase.Report, reportDBConn));
            }

            services.AddSingleton(provider => new MySqlClient(provider, DataBase.Main, config.GetConnectionString($"{serviceType}_MainDB")));
            services.AddSingleton(provider => new MySqlClient(provider, DataBase.Wager, config.GetConnectionString($"{serviceType}_WagerDB")));
            services.AddSingleton(provider => new MySqlClient(provider, DataBase.ReportCenter, config.GetConnectionString($"{serviceType}_ReportCenter")));

            return services;
        }

        public static IServiceCollection AddRedisConnection(this IServiceCollection services, IConfiguration configuration)
        {
            return services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(configuration.GetValue<string>("RedisConnection")))
                            .AddSingleton<ICacheHelper, RedisCacheHelper>();
        }

        public static IServiceCollection AddHangfireService(this IServiceCollection services)
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

        public static IServiceCollection RegisterServices(this IServiceCollection services)
        {
            return services
                .AddSingleton<HttpHelper>()
                .AddSingleton<SecureHelper>()
                .AddSingleton<ExcelHelper>()
                .AddSingleton<TelegramBotHelper>()
                .AddSingleton<AWSCloudWatchLogsHelper>()
                .AddTransient<MainDBDataLayer>()
                .AddTransient<WagerDBDataLayer>()
                .AddTransient<ReportDBDataLayer>()
                .AddTransient<ReportBackupDBDataLayer>()
                .AddTransient<ReportCenterDataLayer>()
                .AddTransient<YashBetDBDataLayer>()
                .AddSingleton<ThirdPartyManager>()
                .AddSingleton<PartitionService>()
                .AddSingleton<FincanceService>()
                .AddSingleton<CustomerService>()
                .AddSingleton<OnlineCountService>()
                .AddSingleton<CustomerAutoLockService>()
                .AddSingleton<CustomerAutoUnFreezeService>()
                .AddSingleton<CustClosingBalanceService>()
                .AddSingleton<MpaUpdateExchangeRateService>()
                .AddSingleton<AffiliateApi>()
                .AddSingleton<UpdateAffiliateInfoService>()
                .AddSingleton<ConfigService>()
                .AddSingleton<ConfigBLL>()
                .AddSingleton<BlockIpService>()
                .AddSingleton<AllowIpService>()
                .AddSingleton<WinLossSummaryService>()
                .AddSingleton<MpaUpdateMonthRateService>()
                .AddSingleton<HotEventService>();
        }

        public static IServiceCollection RegisterHttpClient(this IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();

            var proxyConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<ProxyConfig>("ProxySetting");
            var proxyHost = proxyConfig.ProxyHost;
            var proxyPort = proxyConfig.ProxyPort;

            if (!string.IsNullOrEmpty(proxyHost))
            {
                var proxy = new HttpClientHandler
                {
                    Proxy = new WebProxy(proxyHost, proxyPort),
                    UseProxy = true
                };

                services.AddHttpClient("with_proxy")
                    .ConfigurePrimaryHttpMessageHandler(() => proxy);
            }
            else
            {
                services.AddHttpClient();
            }

            return services;
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

        public static IServiceCollection RegisterScheduledJobs(this IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();
            var configService = provider.GetRequiredService<ConfigService>();

            // 取得啟用的Job
            var enableJobList = configService.Query<JobOption>("Service_ScheduledJob_Get", new { Project = "Hangfire" })
                .Where(r => r.Enable);

            // 依照Job的Group進行分類整理並設定到MyJobManager
            var jobGroups = enableJobList.GroupBy(i => i.Group).ToDictionary(i => i.Key, i => i.ToList());
            services.AddSingleton(serviceProvider => jobGroups);
            MyJobManager.SettingJob(jobGroups);

            // 取得有實作IScheduledJob的Class列表
            var scheduledJobs = typeof(IScheduledJob).Assembly.GetTypes().Where(item => item.GetInterfaces().Contains(typeof(IScheduledJob)));

            // 依照啟用的Job建立Instance
            foreach (var jobOption in enableJobList)
            {
                var job = scheduledJobs.FirstOrDefault(i => string.Equals(i.Name, jobOption.ClassName, StringComparison.OrdinalIgnoreCase));
                if (job != null)
                {
                    services.AddSingleton(serviceProvider => (IScheduledJob)Activator.CreateInstance(job, new object[] { serviceProvider }));
                }
            }

            return services;
        }

        public static IServiceCollection RegisterTpHelper(this IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();
            services.AddSingleton<BaseTpHelper>();
            var tpList = provider.GetRequiredService<ThirdPartyManager>().GetAllTpList();
            services.AddSingleton(provider => tpList);

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

        public static IServiceCollection AddConfig(this IServiceCollection services)
        {
            return services.AddOptionListByDB<Config>("SiteConfig_Get", new { ConfigGroup = "HangfireService" });
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
            var siteConfigGroup = provider.GetRequiredService<IEnumerable<Config>>().GroupBy(i => i.SiteId);

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



