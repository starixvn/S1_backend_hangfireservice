using Hangfire;

using HangfireService.Core.Jobs;
using HangfireService.Extensions;
using HangfireService.Extensions.ActionFilters;

using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Json;

var builder = WebApplication.CreateBuilder(args);

var remoteUrls = builder.Configuration.GetSection("ConfigService").Get<string[]>();
remoteUrls = builder.Configuration.CheckLocalConfig(builder.Environment, args, remoteUrls);
builder.Configuration.AddRemoteJsonConfig(remoteUrls);

builder.Host.UseSerilog((context, Configuration) =>
{
    Configuration
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .Filter.ByExcluding(FilterLog)
        .Enrich.WithExceptionDetails();
    OverrideLoggerWriteToLFile(Configuration);
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
    DashboardTitle = "AILab",
    AppPath = null,
    Authorization = new[] { new LocalAuthorizationFilter() }
});

MyJobManager.AddRecurringJob(app.Services);

var minWorkers = Environment.ProcessorCount * 8;
var minIOCPs = Environment.ProcessorCount * 8;
ThreadPool.SetMinThreads(minWorkers, minIOCPs);

app.Run();
return;

#region Log Setting

bool FilterLog(LogEvent logEvent)
{
    var excludesMessage = builder.Configuration.GetSection("ExcludesMessage").Get<string[]>();
    var excludesApiEndpoints = builder.Configuration.GetSection("ExcludesApiEndpoint").Get<string[]>();

    return logEvent.Properties.Any(p =>
        excludesApiEndpoints.Any(endpoint => p.Value.ToString()!.Contains(endpoint)) ||
        excludesMessage.Any(message => logEvent.MessageTemplate.ToString()!.Contains(message)) ||
        (p.Key == "SourceContext" && p.Value.ToString()!.Contains("Microsoft.AspNetCore.Hosting.Diagnostics")));
}

void OverrideLoggerWriteToLFile(LoggerConfiguration configuration)
{
    const string cashSiteBrand = "Nova";

    var brand = Environment.GetEnvironmentVariable("Brand") ?? string.Empty;
    if (brand != cashSiteBrand) return;

    var logFilePath = $"/hangfire/hangfire-{DateTime.Now:yyyyMMddHHmmssfff}.log";
    configuration
        .WriteTo.File(new JsonFormatter(),
            logFilePath,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 60,
            fileSizeLimitBytes: 10485760,
            rollOnFileSizeLimit: true);
}

#endregion