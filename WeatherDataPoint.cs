namespace FailSafeWeatherMonitor;

public class WeatherDataPoint
{
    public int Id { get; set; }

    public string StationName { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    public decimal Temperature { get; set; }

    public decimal Humidity { get; set; }

    public string DataSource { get; set; } = string.Empty;
}