using MonitorNasdaq.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MonitorNasdaq.Services;

public class DailyReportService : BackgroundService
{
    private readonly MarketDataService _marketData;
    private readonly NotificationService _notification;
    private readonly MonitorSettings _settings;
    private readonly ILogger<DailyReportService> _logger;
    private readonly bool _sendNow;

    private static readonly TimeZoneInfo BeijingTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");

    public DailyReportService(
        MarketDataService marketData,
        NotificationService notification,
        IOptions<MonitorSettings> settings,
        ILogger<DailyReportService> logger)
    {
        _marketData = marketData;
        _notification = notification;
        _settings = settings.Value;
        _logger = logger;
        _sendNow = Environment.GetEnvironmentVariable("SendNow") == "true";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("纳指100日报服务已启动，每日北京时间 {Hour}:00 发送", _settings.ReportHourBeijing);

        if (_sendNow)
        {
            _logger.LogInformation("检测到 --now 参数，立即发送一次日报");
            await SendReportAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextReport();
            _logger.LogInformation("下次发送时间: {NextTime} (北京时间), 等待 {Hours:F1} 小时",
                TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.Add(delay), BeijingTimeZone).ToString("yyyy-MM-dd HH:mm"),
                delay.TotalHours);

            await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested)
                break;

            if (IsAfterTradingDay())
                await SendReportAsync(stoppingToken);
            else
                _logger.LogInformation("今天不是美股交易日的次日，跳过");
        }
    }

    private const int MaxRetries = 3;
    private static readonly TimeSpan RetryInterval = TimeSpan.FromMinutes(5);

    private async Task SendReportAsync(CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var report = await _marketData.GetDailyReportAsync(ct);
                if (report is null)
                {
                    _logger.LogWarning("第 {Attempt}/{Max} 次尝试：未能获取行情数据", attempt, MaxRetries);
                    if (attempt < MaxRetries)
                    {
                        await Task.Delay(RetryInterval, ct);
                        continue;
                    }
                    _logger.LogError("已达最大重试次数，放弃本次发送");
                    return;
                }

                var title = "纳斯达克100指数";
                var content = FormatReport(report);

                _logger.LogInformation("简报内容:\n{Content}", content);
                await _notification.SendAsync(title, content, ct);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "第 {Attempt}/{Max} 次尝试失败", attempt, MaxRetries);
                if (attempt < MaxRetries)
                    await Task.Delay(RetryInterval, ct);
            }
        }
    }

    private static bool IsAfterTradingDay()
    {
        var beijingNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BeijingTimeZone);
        var yesterday = beijingNow.DayOfWeek switch
        {
            DayOfWeek.Monday => DayOfWeek.Friday,
            _ => beijingNow.AddDays(-1).DayOfWeek
        };

        return yesterday is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
    }

    private TimeSpan GetDelayUntilNextReport()
    {
        var utcNow = DateTime.UtcNow;
        var beijingNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, BeijingTimeZone);

        var todayReport = beijingNow.Date.AddHours(_settings.ReportHourBeijing);
        var nextReport = beijingNow < todayReport ? todayReport : todayReport.AddDays(1);

        var nextReportUtc = TimeZoneInfo.ConvertTimeToUtc(nextReport, BeijingTimeZone);
        return nextReportUtc - utcNow;
    }

    private static readonly string[] WeekDays = ["周日", "周一", "周二", "周三", "周四", "周五", "周六"];

    private static string FormatReport(MarketReport report)
    {
        var weekDay = WeekDays[(int)report.TradeDate.DayOfWeek];
        var pointsSign = report.DailyChangePoints >= 0 ? "+" : "";
        var pctSign = report.DailyChangePercent >= 0 ? "+" : "";
        var fromHighSign = report.FromHighPercent >= 0 ? "+" : "";
        var fromLowSign = report.FromLowPercent >= 0 ? "+" : "";

        return
            $"# {report.TradeDate:yyyy-MM-dd}（{weekDay}）  \n" +
            $"## 昨收：{report.YesterdayClose:N2}  \n" +
            $"## 今收：{report.TodayClose:N2}  \n" +
            $"## 变动：{pointsSign}{report.DailyChangePoints:N2}（{pctSign}{report.DailyChangePercent:F2}%）  \n" +
            $"## 52周最高：{report.Week52High:N2}（{fromHighSign}{report.FromHighPercent:F2}%）  \n" +
            $"## 52周最低：{report.Week52Low:N2}（{fromLowSign}{report.FromLowPercent:F2}%）";
    }
}
