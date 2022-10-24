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
using HangfireService.Core.Promotion;

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

        public static IServiceCollection AddHangfireService(this IServiceCollection services, IConfiguration configuration)
        {       
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
                var jobGroups = configuration.GetSection("ScheduledJob").Get<Dictionary<string, List<JobOption>>>();
                var queues= jobGroups.Select(item => item.Key.ToLower()).ToList();
                queues.Add("default");
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
                .AddTransient<MainDBDataLayer>()
                .AddTransient<WagerDBDataLayer>()
                .AddTransient<ReportDBDataLayer>()
                .AddSingleton<ThirdPartyManager>()
                .AddSingleton<PartitionService>()
                .AddSingleton<FincanceService>()
                .AddSingleton<PtMerchantBalanceCheckService>()
                .AddSingleton<WorldCupDepositDailySummaryService>()
                .AddSingleton<ConfigService>();
        }

        public static IServiceCollection RegisterPromotion(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddScoped<IBonusSummaryTimerService, BonusSummaryTimerService>()
                .AddScoped<IReferralPromotionService, ReferralPromotionService>()
                .AddScoped<IReferralFreeCreditPromotionService, ReferralFreeCreditPromotionService>()
                .AddScoped<IVipUpgradePromotionService, VipUpgradePromotionService>()
                .AddScoped<IOtherPromotionService, OtherPromotionService>()
                .AddScoped<PromotionSummaryBase>(
                    serviceProvider => 
                    {
                        var timerService = serviceProvider.GetRequiredService<IBonusSummaryTimerService>();
                        var referralService = serviceProvider.GetRequiredService<IReferralPromotionService>();
                        return new ReferralPromotion(timerService, referralService, WorkerType.Referral);
                    })
                .AddScoped<PromotionSummaryBase>(
                    serviceProvider => 
                    {
                        var timerService = serviceProvider.GetRequiredService<IBonusSummaryTimerService>();
                        var referralFreeCreditService = serviceProvider.GetRequiredService<IReferralFreeCreditPromotionService>();
                        return new ReferralFreeCreditPromotion(timerService, referralFreeCreditService, WorkerType.ReferralFreeCredit);
                    })
                .AddScoped<PromotionSummaryBase>(
                    serviceProvider => 
                    {
                        var timerService = serviceProvider.GetRequiredService<IBonusSummaryTimerService>();
                        var vipUpgradeService = serviceProvider.GetRequiredService<IVipUpgradePromotionService>();
                        return new VipUpgradePromotion(timerService, vipUpgradeService, WorkerType.VipUpgrade);
                    })
                .AddScoped<PromotionSummaryBase>(
                    serviceProvider => 
                    {
                        var timerService = serviceProvider.GetRequiredService<IBonusSummaryTimerService>();
                        var otherService = serviceProvider.GetRequiredService<IOtherPromotionService>();
                        return new OtherPromotion(timerService, otherService, WorkerType.Other);
                    });
            return services;
        }

        public static IServiceCollection RegisterScheduledJobs(this IServiceCollection services, IConfiguration configuration)
        {
            MyJobManager.SettingJob(configuration);

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

        public static IServiceCollection AddOptionListByDB<T>(this IServiceCollection services, string sql, object parameters = null)
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
    }
}
