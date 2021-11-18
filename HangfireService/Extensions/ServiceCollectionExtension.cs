using Hangfire;
using Hangfire.Console.Extensions;
using Hangfire.Dashboard.Management;
using Hangfire.MemoryStorage;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Helpers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.ThirdParty;
using HangfireService.Core.ThirdParty.Interfaces;
using System.Net;

namespace HangfireService.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddDatabaseConnection(this IServiceCollection services, IConfiguration configuration)
        {
            var provider = services.BuildServiceProvider();
            var logger = provider.GetRequiredService<ILogger<MySqlClient>>();

            return services
            .AddSingleton(param => new MySqlClient(configuration.GetConnectionString("MainDB"), logger)
            {
                DataBase = DataBase.Main
            })
            .AddSingleton(param => new MySqlClient(configuration.GetConnectionString("ReportDB"), logger)
            {
                DataBase = DataBase.Report
            })
            .AddSingleton(param => new MySqlClient(configuration.GetConnectionString("WagerDB"), logger)
            {
                DataBase = DataBase.Wager
            });
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
                config.Queues = jobGroups.Select(item => item.Key.ToLower()).ToArray();
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
            .AddTransient<MainDBDataLayer>()
            .AddTransient<WagerDBDataLayer>()
            .AddTransient<ReportDBDataLayer>()
            .AddSingleton<ThirdPartyManager>();
        }

        public static IServiceCollection RegisterScheduledJobs(this IServiceCollection services, IConfiguration configuration)
        {
            var jobGroups = configuration.GetSection("ScheduledJob").Get<Dictionary<string, List<JobOption>>>();
            services.AddSingleton(provider => jobGroups);

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
            var tpList = provider.GetRequiredService<ThirdPartyManager>().GetAllTpList();
            services.AddSingleton(provider => tpList);

            var tpTypes = typeof(ITpHelper).Assembly.GetTypes().Where(item => item.GetInterfaces().Contains(typeof(ITpHelper)));
            foreach (var tp in tpTypes)
            {
                services.AddSingleton(provider => (ITpHelper)Activator.CreateInstance(tp, new object[] { provider }));
            }

            return services;
        }
    }
}
