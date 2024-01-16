using Hangfire;

using HangfireService.Core.Jobs;
using HangfireService.Extensions;
using HangfireService.Extensions.ActionFilters;

using Serilog;
using Serilog.Exceptions;

var builder = WebApplication.CreateBuilder(args);

var remoteUrls = builder.Configuration.GetSection("ConfigService").Get<string[]>();
remoteUrls = builder.Configuration.CheckLocalConfig(builder.Environment, args, remoteUrls);
builder.Configuration.AddRemoteJsonConfig(remoteUrls);

var excludesMessage = builder.Configuration.GetSection("ExcludesMessage").Get<string[]>();
var excludesApiEndpoints = builder.Configuration.GetSection("ExcludesApiEndpoint").Get<string[]>();
builder.Host.UseSerilog((context, Configuration) =>
{
    Configuration
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .Filter.ByExcluding(c =>
            c.Properties.Any(p =>
                excludesApiEndpoints.Any(item => p.Value.ToString().Contains(item))
                || excludesMessage.Any(item => c.MessageTemplate.ToString().Contains(item))))
        .Enrich.WithExceptionDetails();
});

// Add services to the container.

builder.Services
    .AddDatabaseConnection(builder.Configuration)
    .AddRedisConnection(builder.Configuration)
    .RegisterServices()
    .RegisterPromotion()
    .RegisterScheduledJobs()
    .AddHangfireService()
    .RegisterTpHelper()
    .AddOption()
    .AddConfig()
    .AddServerInfo()
    .AddSiteConfig()
    .RegisterHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.EnvironmentName.Equals("Local"))
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapGet("/health", () => "OK.");
});

app.UseHangfireDashboard(pathMatch: "", options: new DashboardOptions()
{
    DashboardTitle = "AI.Lab",
    AppPath = null,
    Authorization = new[] { new LocalAuthorizationFilter() }
});

MyJobManager.AddRecurringJob(app.Services);

var minWorkers = Environment.ProcessorCount * 8;
var minIOCPs = Environment.ProcessorCount * 8;
ThreadPool.SetMinThreads(minWorkers, minIOCPs);

app.Run();