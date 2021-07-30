using Hangfire;
using Hangfire.Console.Extensions;
using Hangfire.MemoryStorage;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Helpers;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Enums;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.ThirdParty;
using HangfireService.Core.ThirdParty.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace HangfireService.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddDatabaseConnection(this IServiceCollection services, IConfiguration configuration)
        {
            var provider = services.BuildServiceProvider();
            var logger = provider.GetRequiredService<ILogger<MySqlClient>>();

            services.AddSingleton(param => new MySqlClient(configuration.GetConnectionString("ConnectionString"), logger)
            {
                DataBase = DataBase.Main
            });
            services.AddSingleton(param => new MySqlClient(configuration.GetConnectionString("ConnectionStringReport"), logger)
            {
                DataBase = DataBase.Report
            });
            services.AddSingleton(param => new MySqlClient(configuration.GetConnectionString("ConnectionStringWager"), logger)
            {
                DataBase = DataBase.Wager
            });
            return services;
        }

        public static IServiceCollection AddHangfireService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHangfire(config =>
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
                 //.UseManagementPages(p => p.AddJobs(() => ScheduledJobManager.GetModuleTypes(services.BuildServiceProvider())))
                .UseSerilogLogProvider()
                .UseMemoryStorage();
            });
            services.AddHangfireConsoleExtensions();
            services.AddHangfireServer(config =>
            {
                config.SchedulePollingInterval = TimeSpan.FromSeconds(15);
                config.WorkerCount = 1;
            });
            return services;
        }

        public static IServiceCollection RegisterServices(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration.GetValue<string>("ProxyHost") != null)
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

            services.AddSingleton<HttpHelper>();
            services.AddTransient<MainDBDataLayer>();
            services.AddTransient<WagerDBDataLayer>();
            services.AddTransient<ReportDBDataLayer>();
            services.AddSingleton<ThirdPartyManager>();

            return services;
        }

        public static IServiceCollection RegisterScheduledJobs(this IServiceCollection services, IConfiguration configuration)
        {
            var jobOptions = new List<JobDetailOption>();
            configuration.GetSection("Jobs").Bind(jobOptions);
            services.AddSingleton(provider => jobOptions);

            var jobTypes = typeof(IScheduledJob).Assembly.GetTypes().Where(item => item.GetInterfaces().Contains(typeof(IScheduledJob)));
            foreach (var option in jobOptions)
            {
                if (option.Enable && option.Type == typeof(IScheduledJob).Name)
                {
                    var jobType = jobTypes.First(jobType => jobType.Name.Equals(option.Name, StringComparison.OrdinalIgnoreCase));
                    services.AddSingleton(provider => (IScheduledJob)Activator.CreateInstance(jobType, new object[] { provider }));
                }
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
