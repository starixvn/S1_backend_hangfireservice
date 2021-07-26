using Hangfire;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Jobs;
using HangfireService.Core.Jobs.Interfaces;
using HangfireService.Core.Models.Jobs;
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
                })
                .AddHangfireService(Configuration)
                .RegisterScheduledJobs(services =>
                {
                    var jobs = new List<JobDetailOption>();
                    Configuration.GetSection("Jobs").Bind(jobs);
                    services.AddSingleton(provider => jobs);

                    foreach (var job in jobs)
                    {
                        if (job.Enable && job.Type == typeof(IScheduledJob).Name)
                        {
                            var assembly = Assembly.Load($"HangfireService.Core");
                            var assemblyType = assembly.GetType($"HangfireService.Core.Jobs.{job.Name}");
                            services.AddSingleton(provider => (IScheduledJob)Activator.CreateInstance(assemblyType, new object[] { provider }));
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
