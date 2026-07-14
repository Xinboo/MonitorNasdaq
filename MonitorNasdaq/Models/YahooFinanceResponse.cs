using System.Text.Json.Serialization;

namespace MonitorNasdaq.Models;

public class YahooFinanceResponse
{
    [JsonPropertyName("chart")]
    public ChartData? Chart { get; set; }
}

public class ChartData
{
    [JsonPropertyName("result")]
    public List<ChartResult>? Result { get; set; }
}

public class ChartResult
{
    [JsonPropertyName("timestamp")]
    public List<long>? Timestamp { get; set; }

    [JsonPropertyName("indicators")]
    public Indicators? Indicators { get; set; }
}

public class Indicators
{
    [JsonPropertyName("quote")]
    public List<QuoteIndicator>? Quote { get; set; }
}

public class QuoteIndicator
{
    [JsonPropertyName("close")]
    public List<double?>? Close { get; set; }
}
