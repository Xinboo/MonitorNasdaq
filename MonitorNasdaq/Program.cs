using MonitorNasdaq.Configuration;
using MonitorNasdaq.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

if (args.Contains("--now"))
    Environment.SetEnvironmentVariable("SendNow", "true");

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<MonitorSettings>(builder.Configuration.GetSection("Monitor"));
builder.Services.AddHttpClient<MarketDataService>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        UseCookies = true,
        CookieContainer = new System.Net.CookieContainer()
    });
builder.Services.AddHttpClient<NotificationService>();
builder.Services.AddHostedService<DailyReportService>();

var host = builder.Build();
await host.RunAsync();
