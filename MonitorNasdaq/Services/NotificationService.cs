using MonitorNasdaq.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MonitorNasdaq.Services;

public class NotificationService
{
    private readonly HttpClient _httpClient;
    private readonly MonitorSettings _settings;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(HttpClient httpClient, IOptions<MonitorSettings> settings, ILogger<NotificationService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(string title, string content, CancellationToken ct = default)
    {
        var keys = _settings.ServerChanKeys.Where(k => !string.IsNullOrEmpty(k)).ToList();

        if (keys.Count == 0)
        {
            _logger.LogWarning("未配置 Server酱 Key，跳过推送");
            return;
        }

        var encodedTitle = Uri.EscapeDataString(title);
        var encodedContent = Uri.EscapeDataString(content);

        foreach (var key in keys)
        {
            var url = $"https://sctapi.ftqq.com/{key}.send?title={encodedTitle}&desp={encodedContent}";

            _logger.LogInformation("正在发送通知: {Title} -> {Key}", title, key[..8] + "***");

            var response = await _httpClient.GetAsync(url, ct);

            if (response.IsSuccessStatusCode)
                _logger.LogInformation("通知发送成功 -> {Key}", key[..8] + "***");
            else
                _logger.LogWarning("通知发送失败 -> {Key}, 状态码: {StatusCode}", key[..8] + "***", response.StatusCode);
        }
    }
}
