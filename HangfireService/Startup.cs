using Hangfire;
using HangfireService.Core.Jobs;
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
                .AddAdminSiteAuth()
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
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseHangfireDashboard(pathMatch: "", options: new DashboardOptions()
            {
                DashboardTitle = "AI.Lab",
                AppPath = Configuration.GetValue<string>("AdminSiteUrl"),
                Authorization = new[] { new LocalAuthorizationFilter() }
            });
            ScheduledJobManager.Setup(provider, Configuration);

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
