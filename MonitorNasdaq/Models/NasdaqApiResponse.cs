using System.Text.Json.Serialization;

namespace MonitorNasdaq.Models;

public class NasdaqInfoResponse
{
    [JsonPropertyName("data")]
    public NasdaqInfoData? Data { get; set; }
}

public class NasdaqInfoData
{
    [JsonPropertyName("primaryData")]
    public NasdaqPrimaryData? PrimaryData { get; set; }

    [JsonPropertyName("keyStats")]
    public NasdaqKeyStats? KeyStats { get; set; }
}

public class NasdaqPrimaryData
{
    [JsonPropertyName("lastSalePrice")]
    public string? LastSalePrice { get; set; }

    [JsonPropertyName("netChange")]
    public string? NetChange { get; set; }

    [JsonPropertyName("percentageChange")]
    public string? PercentageChange { get; set; }

    [JsonPropertyName("lastTradeTimestamp")]
    public string? LastTradeTimestamp { get; set; }
}

public class NasdaqKeyStats
{
    [JsonPropertyName("previousclose")]
    public NasdaqStatItem? PreviousClose { get; set; }
}

public class NasdaqStatItem
{
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

public class NasdaqChartResponse
{
    [JsonPropertyName("data")]
    public NasdaqChartData? Data { get; set; }
}

public class NasdaqChartData
{
    [JsonPropertyName("chart")]
    public List<NasdaqChartPoint>? Chart { get; set; }
}

public class NasdaqChartPoint
{
    [JsonPropertyName("y")]
    public double Y { get; set; }
}
