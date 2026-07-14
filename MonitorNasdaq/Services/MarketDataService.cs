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
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    public async Task<MarketReport?> GetDailyReportAsync(CancellationToken ct = default)
    {
        var symbol = Uri.EscapeDataString(_settings.Symbol);
        var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{symbol}?range=1y&interval=1d";

        _logger.LogInformation("正在获取 {Symbol} 行情数据...", _settings.Symbol);

        var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var data = JsonSerializer.Deserialize<YahooFinanceResponse>(json);

        var closes = data?.Chart?.Result?.FirstOrDefault()?.Indicators?.Quote?.FirstOrDefault()?.Close;
        var timestamps = data?.Chart?.Result?.FirstOrDefault()?.Timestamp;

        if (closes is null || timestamps is null || closes.Count < 2)
        {
            _logger.LogWarning("未获取到有效的行情数据");
            return null;
        }

        var validCloses = closes.Where(c => c.HasValue).Select(c => c!.Value).ToList();
        if (validCloses.Count < 2)
            return null;

        var todayClose = validCloses[^1];
        var yesterdayClose = validCloses[^2];
        var week52High = validCloses.Max();
        var dailyChange = (todayClose - yesterdayClose) / yesterdayClose * 100;
        var fromHigh = (todayClose - week52High) / week52High * 100;

        var lastTimestamp = timestamps[^1];
        var tradeDate = DateTimeOffset.FromUnixTimeSeconds(lastTimestamp).UtcDateTime.Date;

        return new MarketReport
        {
            TradeDate = tradeDate,
            TodayClose = todayClose,
            YesterdayClose = yesterdayClose,
            DailyChangePercent = dailyChange,
            Week52High = week52High,
            FromHighPercent = fromHigh
        };
    }
}

public class MarketReport
{
    public DateTime TradeDate { get; set; }
    public double TodayClose { get; set; }
    public double YesterdayClose { get; set; }
    public double DailyChangePercent { get; set; }
    public double Week52High { get; set; }
    public double FromHighPercent { get; set; }
}
