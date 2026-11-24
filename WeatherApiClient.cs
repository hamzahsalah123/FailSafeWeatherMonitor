using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;

namespace FailSafeWeatherMonitor;

public class WeatherApiClient
{
    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(20)
    };

    public void FetchAndSave()
    {
        const string url =
            "https://api.open-meteo.com/v1/forecast" +
            "?latitude=57.6832&longitude=11.9511" +
            "&current=temperature_2m,relative_humidity_2m" +
            "&timezone=Europe%2FStockholm";

        try
        {
            using var response = Client.GetAsync(url)
                .GetAwaiter().GetResult();

            response.EnsureSuccessStatusCode();

            string json = response.Content.ReadAsStringAsync()
                .GetAwaiter().GetResult();

            var result = JsonSerializer.Deserialize<WeatherResponse>(json);

            if (result?.current == null ||
                !DateTime.TryParseExact(
                    result.current.time,
                    "yyyy-MM-dd'T'HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime timestamp) ||
                result.current.temperature_2m == null ||
                result.current.relative_humidity_2m == null)
            {
                AnsiConsole.MarkupLine(
                    "[red]API-svaret saknar giltiga vädervärden.[/]");
                return;
            }

            decimal temperature = result.current.temperature_2m.Value;
            decimal humidity = result.current.relative_humidity_2m.Value;

            if (temperature < -90 || temperature > 60 ||
                humidity < 0 || humidity > 100)
            {
                AnsiConsole.MarkupLine(
                    "[red]API-värdena ligger utanför rimliga gränser.[/]");
                return;
            }

            using var database = new WeatherDbContext();

            const string stationName = "Goteborg_Avenyn";

            bool exists = database.WeatherDataPoints.Any(p =>
                p.StationName == stationName &&
                p.Timestamp == timestamp);

            if (exists)
            {
                AnsiConsole.MarkupLine(
                    "[yellow]Mätningen finns redan i databasen.[/]");
            }
            else
            {
                database.WeatherDataPoints.Add(new WeatherDataPoint
                {
                    StationName = stationName,
                    Timestamp = timestamp,
                    Temperature = temperature,
                    Humidity = humidity,
                    DataSource = "API"
                });

                database.SaveChanges();

                AnsiConsole.MarkupLine(
                    "[green]Vädermätningen sparades.[/]");
            }

            var table = new Table();
            table.AddColumn("Station");
            table.AddColumn("Tid, svensk lokal tid");
            table.AddColumn("Temperatur");
            table.AddColumn("Luftfuktighet");

            table.AddRow(
                stationName,
                timestamp.ToString("yyyy-MM-dd HH:mm"),
                $"{temperature:F3} °C",
                $"{humidity:F1} %");

            AnsiConsole.Write(table);
        }
        catch (HttpRequestException)
        {
            AnsiConsole.MarkupLine(
                "[red]Kunde inte hämta vädret. Kontrollera internet och försök igen.[/]");
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine(
                "[red]API-anropet tog för lång tid. Försök igen.[/]");
        }
        catch (JsonException)
        {
            AnsiConsole.MarkupLine(
                "[red]API-svaret kunde inte tolkas.[/]");
        }
        catch (Exception ex) when (
            ex is DbUpdateException ||
            ex is Microsoft.Data.SqlClient.SqlException ||
            ex is InvalidOperationException)
        {
            AnsiConsole.MarkupLine(
                "[red]Databasfel. Kontrollera anslutningen och försök igen.[/]");
        }
    }

    public class WeatherResponse
    {
        public CurrentWeather? current { get; set; }
    }

    public class CurrentWeather
    {
        public string? time { get; set; }

        public decimal? temperature_2m { get; set; }

        public decimal? relative_humidity_2m { get; set; }
    }
}