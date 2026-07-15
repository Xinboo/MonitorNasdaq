namespace MonitorNasdaq.Configuration;

public class MonitorSettings
{
    public string Symbol { get; set; } = "NDX";
    public string ServerChanKey { get; set; } = "";
    public int ReportHourBeijing { get; set; } = 9;
}
