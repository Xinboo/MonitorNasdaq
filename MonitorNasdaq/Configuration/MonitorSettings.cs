namespace MonitorNasdaq.Configuration;

public class MonitorSettings
{
    public string Symbol { get; set; } = "NDX";
    public List<string> ServerChanKeys { get; set; } = [];
    public int ReportHourBeijing { get; set; } = 9;
}
