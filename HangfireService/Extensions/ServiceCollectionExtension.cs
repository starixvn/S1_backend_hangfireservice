using Hangfire;
using Hangfire.Console.Extensions;
using Hangfire.MemoryStorage;
using HangfireService.Core.DataLayers;
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
                .UseDefaultTypeSerializer()
                .UseSerilogLogProvider()
                .UseMemoryStorage(
                   // new MemoryStorageOptions() { JobExpirationCheckInterval = TimeSpan.FromSeconds(5) }
                    );
            });
            services.AddHangfireConsoleExtensions();
            services.AddHangfireServer(config =>
            {
                //config.SchedulePollingInterval = TimeSpan.FromSeconds(5);
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
