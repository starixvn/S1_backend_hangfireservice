using Hangfire;
using Hangfire.Console;
using Hangfire.Console.Extensions;
using Hangfire.Dashboard.Management;
using Hangfire.MemoryStorage;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs;
using HangfireService.Core.Models.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

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
                config.SchedulePollingInterval = TimeSpan.FromSeconds(5);
                config.WorkerCount = 1;
            });
            return services;
        }

        public static IServiceCollection RegisterServices(this IServiceCollection serviceCollection, Action<IServiceCollection> configureDelegate)
        {
            configureDelegate(serviceCollection);
            return serviceCollection;
        }

        public static IServiceCollection RegisterScheduledJobs(this IServiceCollection serviceCollection, Action<IServiceCollection> configureDelegate)
        {
            configureDelegate(serviceCollection);
            return serviceCollection;
        }
    }
}
