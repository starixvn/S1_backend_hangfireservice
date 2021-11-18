using Hangfire;
using HangfireService.Core.Jobs;
using HangfireService.Extensions;
using HangfireService.Extensions.ActionFilters;
using Serilog;
using Serilog.Exceptions;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddRemoteJsonConfig(builder.Configuration.GetValue<string>("ConfigService"));

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
    .RegisterServices(builder.Configuration)
    .AddHangfireService(builder.Configuration)
    .RegisterScheduledJobs(builder.Configuration)
    .RegisterTpHelper();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.EnvironmentName.Equals("Local"))
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseHangfireDashboard(pathMatch: "", options: new DashboardOptions()
{
    DashboardTitle = "AI.Lab",
    AppPath = null,
    Authorization = new[] { new LocalAuthorizationFilter() }
});
ScheduledJobManager.Setup(app.Services);

app.UseEndpoints(endpoints =>
{
    endpoints.MapGet("/", async context =>
    {
        await context.Response.WriteAsync("OK.");
    });
});

app.Run();