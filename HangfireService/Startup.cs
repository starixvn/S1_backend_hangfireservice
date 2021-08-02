using Hangfire;
using HangfireService.Core.DataLayers;
using HangfireService.Core.Helpers;
using HangfireService.Core.Jobs;
using HangfireService.Core.ThirdParty;
using HangfireService.Extensions;
using HangfireService.Extensions.ActionFilters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

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
                .RegisterServices(Configuration)
                .AddHangfireService(Configuration)
                .RegisterScheduledJobs(Configuration)
                .RegisterTpHelper();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider provider)
        {
            if (env.EnvironmentName.Equals("Local"))
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseHangfireDashboard(options: new DashboardOptions()
            {
                DashboardTitle = "AI.Lab",
                AppPath = Configuration.GetValue<string>("AdminSiteUrl"),
                Authorization = new[] { new AdminSiteAuthorizationFilter() }
            });

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
