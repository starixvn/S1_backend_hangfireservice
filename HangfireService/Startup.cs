using Hangfire;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
using HangfireService.Core.ThirdParty;
using HangfireService.Core.ThirdParty.Interfaces;
using HangfireService.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HangfireService
{
    public class Startup
    {
        public Startup(IConfiguration configuration) => Configuration = configuration;

        public IConfiguration Configuration { get; }
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddDatabaseConnection(Configuration)
                .RegisterServices(services =>
                {
                    services.AddTransient(provider => new MainDBDataLayer(provider));
                    services.AddTransient(provider => new WagerDBDataLayer(provider));
                    services.AddTransient(provider => new ReportDBDataLayer(provider));
                    services.AddSingleton(provider => new ThirdPartyManager(provider));

                    var tpTypes = typeof(ITpBetRecordService).Assembly.GetTypes().Where(item =>item.GetInterfaces().Contains(typeof(ITpBetRecordService)));
                    foreach (var tp in tpTypes)
                    {
                        services.AddSingleton(provider => (ITpBetRecordService)Activator.CreateInstance(tp, new object[] { provider }));
                    }               
                })
                .AddHangfireService(Configuration)
                .RegisterScheduledJobs(services =>
                {
                    var jobOptions = new List<JobDetailOption>();
                    Configuration.GetSection("Jobs").Bind(jobOptions);
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
                });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider provider)
        {
            if (env.EnvironmentName.Equals("Local"))
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseHangfireDashboard(options: new DashboardOptions() { DashboardTitle = "AI.Lab" });

            ScheduledJobManager.Setup(provider);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("OK.");
                });
            });
        }
    }
}
