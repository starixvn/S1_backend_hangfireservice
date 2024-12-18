using Hangfire;
using Hangfire.Dashboard.BasicAuthorization;
using HangfireService.Core.Jobs;
using HangfireService.Extensions;
using HangfireService.Extensions.ActionFilters;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Json;
using Serilog.Sinks.Network;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json",
        optional: true,
        reloadOnChange: false);
var remoteUrls = GetRemoteUrls();

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
        .Enrich.WithExceptionDetails()
        .Enrich.WithProperty("ApplicationName", builder.Configuration.GetValue<string>("ServiceName"))
        .Enrich.WithProperty("Env", builder.Configuration.GetValue<string>("env"))
        .WriteTo.UDPSink(
            builder.Configuration.GetValue<string>("OpenSearch:Uri"),
            builder.Configuration.GetValue<int>("OpenSearch:Port"),
            new JsonFormatter())
        .WriteTo.Sentry(o =>
        {
            // Debug and higher are stored as breadcrumbs (default is Information)
            o.MinimumBreadcrumbLevel = LogEventLevel.Debug;
            // Warning and higher is sent as event (default is Error)
            o.MinimumEventLevel = LogEventLevel.Error;
            o.Dsn = builder.Configuration.GetValue<string>("Sentry:Dsn");
            o.Environment = builder.Configuration.GetValue<string>("Sentry:Environment");
        });

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
    .RegisterSabaplusTpHelper()
    //.RegisterTpHelper()
    .AddOption()
    .AddConfig()
    .AddServerInfo()
    .AddSiteConfig()
    .RegisterHttpClient(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!builder.Environment.IsProduction())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapGet("/health", () => $"{builder.Environment.EnvironmentName}-OK.");
});

app.UseHangfireDashboard(pathMatch: "", options: new DashboardOptions()
{
    DashboardTitle = "AILab",
    AppPath = null,
    Authorization = new[] { new BasicAuthAuthorizationFilter(new BasicAuthAuthorizationFilterOptions
        {
            RequireSsl = false,
            SslRedirect = false,
            LoginCaseSensitive = true,
            Users = new []
            {
                new BasicAuthAuthorizationUser
                {
                    Login = builder.Configuration.GetValue<string>("HangFireAuth:Username"),
                    PasswordClear =  builder.Configuration.GetValue<string>("HangFireAuth:Password"),
                }
            }

        }) }
});


var isGettingJobsFromAppSettings = builder.Configuration.GetSection("IsGettingJobsFromAppSettings").Get<bool>();

if (isGettingJobsFromAppSettings)
{
    var jobNames = builder.Configuration.GetSection("JobNames").Get<List<string>>();
    MyJobManager.SetJobNames(jobNames);
}


MyJobManager.AddRecurringJob(app.Services, builder.Configuration);

var minWorkers = Environment.ProcessorCount * 8;
var minIOCPs = Environment.ProcessorCount * 8;
ThreadPool.SetMinThreads(minWorkers, minIOCPs);

app.Run();
return;

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

string[] GetRemoteUrls()
{
    return builder.Configuration.GetSection("ConfigService").Get<string[]>();
}