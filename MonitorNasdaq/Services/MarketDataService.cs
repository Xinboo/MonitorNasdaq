using System.Globalization;
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
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
    }

    public async Task<MarketReport?> GetDailyReportAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("正在从 Nasdaq 官方 API 获取 {Symbol} 行情数据...", _settings.Symbol);

        var infoUrl = $"https://api.nasdaq.com/api/quote/{_settings.Symbol}/info?assetclass=index";
        var infoResponse = await _httpClient.GetAsync(infoUrl, ct);
        infoResponse.EnsureSuccessStatusCode();
        var infoJson = await infoResponse.Content.ReadAsStringAsync(ct);
        var info = JsonSerializer.Deserialize<NasdaqInfoResponse>(infoJson);

        var primaryData = info?.Data?.PrimaryData;
        var keyStats = info?.Data?.KeyStats;
        if (primaryData?.LastSalePrice is null || keyStats?.PreviousClose?.Value is null)
        {
            _logger.LogWarning("未获取到有效的行情数据");
            return null;
        }

        var todayClose = ParseNumber(primaryData.LastSalePrice);
        var yesterdayClose = ParseNumber(keyStats.PreviousClose.Value);
        if (todayClose == 0 || yesterdayClose == 0)
        {
            _logger.LogWarning("解析价格数据失败");
            return null;
        }

        var eastern = GetEasternTimeZone();
        var tradeDate = DateTime.Now;
        if (primaryData.LastTradeTimestamp != null)
        {
            if (DateTime.TryParseExact(primaryData.LastTradeTimestamp, "MMM d, yyyy",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                tradeDate = parsed;
        }

        var oneYearAgo = tradeDate.AddYears(-1);
        var chartUrl = $"https://api.nasdaq.com/api/quote/{_settings.Symbol}/chart?assetclass=index" +
                       $"&fromdate={oneYearAgo:yyyy-MM-dd}&todate={tradeDate:yyyy-MM-dd}";
        var chartResponse = await _httpClient.GetAsync(chartUrl, ct);
        chartResponse.EnsureSuccessStatusCode();
        var chartJson = await chartResponse.Content.ReadAsStringAsync(ct);
        var chart = JsonSerializer.Deserialize<NasdaqChartResponse>(chartJson);

        var points = chart?.Data?.Chart;
        if (points is null || points.Count == 0)
        {
            _logger.LogWarning("未获取到历史数据，无法计算52周最高/最低");
            return null;
        }

        var week52High = points.Max(p => p.Y);
        var week52Low = points.Min(p => p.Y);

        var dailyChangePoints = todayClose - yesterdayClose;
        var dailyChange = dailyChangePoints / yesterdayClose * 100;
        var fromHigh = (todayClose - week52High) / week52High * 100;
        var fromLow = (todayClose - week52Low) / week52Low * 100;

        _logger.LogInformation("数据获取成功: 收盘 {Close}, 52周高 {High}, 52周低 {Low}",
            todayClose.ToString("F2"), week52High.ToString("F2"), week52Low.ToString("F2"));

        return new MarketReport
        {
            TradeDate = tradeDate,
            TodayClose = todayClose,
            YesterdayClose = yesterdayClose,
            DailyChangePoints = dailyChangePoints,
            DailyChangePercent = dailyChange,
            Week52High = week52High,
            Week52Low = week52Low,
            FromHighPercent = fromHigh,
            FromLowPercent = fromLow
        };
    }

    private static double ParseNumber(string value)
    {
        var cleaned = value.Replace(",", "").Replace("+", "").Replace("%", "").Trim();
        return double.TryParse(cleaned, CultureInfo.InvariantCulture, out var result) ? result : 0;
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
