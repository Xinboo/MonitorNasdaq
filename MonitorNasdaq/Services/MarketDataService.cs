using System.Text.Json;
using MonitorNasdaq.Configuration;
using MonitorNasdaq.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MonitorNasdaq.Services;

public class MarketDataService
{
    private readonly HttpClient _httpClient;
    private readonly MonitorSettings _settings;
    private readonly ILogger<MarketDataService> _logger;

    public MarketDataService(HttpClient httpClient, IOptions<MonitorSettings> settings, ILogger<MarketDataService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Cookie", "GUC=AQEBAgFn; A3=d=AQABBBhQ");
    }

    public async Task<MarketReport?> GetDailyReportAsync(CancellationToken ct = default)
    {
        var symbol = Uri.EscapeDataString(_settings.Symbol);
        var url = $"https://query2.finance.yahoo.com/v8/finance/chart/{symbol}?range=5d&interval=1d";

        _logger.LogInformation("正在获取 {Symbol} 行情数据...", _settings.Symbol);

        var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var data = JsonSerializer.Deserialize<YahooFinanceResponse>(json);

        var result = data?.Chart?.Result?.FirstOrDefault();
        var closes = result?.Indicators?.Quote?.FirstOrDefault()?.Close;
        var timestamps = result?.Timestamp;
        var week52High = result?.Meta?.FiftyTwoWeekHigh;
        var week52Low = result?.Meta?.FiftyTwoWeekLow;

        if (closes is null || timestamps is null || closes.Count < 2 || week52High is null || week52Low is null)
        {
            _logger.LogWarning("未获取到有效的行情数据");
            return null;
        }

        var validCloses = closes.Where(c => c.HasValue).Select(c => c!.Value).ToList();
        if (validCloses.Count < 2)
            return null;

        var todayClose = validCloses[^1];
        var yesterdayClose = validCloses[^2];
        var dailyChangePoints = todayClose - yesterdayClose;
        var dailyChange = dailyChangePoints / yesterdayClose * 100;
        var fromHigh = (todayClose - week52High.Value) / week52High.Value * 100;
        var fromLow = (todayClose - week52Low.Value) / week52Low.Value * 100;

        var lastTimestamp = timestamps[^1];
        var eastern = GetEasternTimeZone();
        var tradeDate = TimeZoneInfo.ConvertTimeFromUtc(
            DateTimeOffset.FromUnixTimeSeconds(lastTimestamp).UtcDateTime, eastern).Date;

        return new MarketReport
        {
            TradeDate = tradeDate,
            TodayClose = todayClose,
            YesterdayClose = yesterdayClose,
            DailyChangePoints = dailyChangePoints,
            DailyChangePercent = dailyChange,
            Week52High = week52High.Value,
            Week52Low = week52Low.Value,
            FromHighPercent = fromHigh,
            FromLowPercent = fromLow
        };
    }

    private static TimeZoneInfo GetEasternTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        }
    }
}

public class MarketReport
{
    public DateTime TradeDate { get; set; }
    public double TodayClose { get; set; }
    public double YesterdayClose { get; set; }
    public double DailyChangePoints { get; set; }
    public double DailyChangePercent { get; set; }
    public double Week52High { get; set; }
    public double Week52Low { get; set; }
    public double FromHighPercent { get; set; }
    public double FromLowPercent { get; set; }
}
