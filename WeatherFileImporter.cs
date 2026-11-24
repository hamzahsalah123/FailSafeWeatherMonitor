using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;

namespace FailSafeWeatherMonitor;

public class WeatherFileImporter
{
    public void Import()
    {
        string filePath = Path.Combine(
            AppContext.BaseDirectory, "weather_data.csv");

        string logPath = Path.Combine(
            AppContext.BaseDirectory, "bad_data.log");

        int imported = 0;
        int rejected = 0;
        int duplicates = 0;

        try
        {
            using var reader = new StreamReader(filePath);
            using var log = new StreamWriter(logPath, append: false);
            using var database = new WeatherDbContext();

            string? header = reader.ReadLine();

            if (header != "StationName\tTimestamp\tTemperature\tHumidity")
            {
                AnsiConsole.MarkupLine("[red]Filen har fel kolumnrubriker.[/]");
                return;
            }

            string? line;

            while ((line = reader.ReadLine()) != null)
            {
                if (!TryParseLine(line, out var point))
                {
                    log.WriteLine(line);
                    rejected++;
                    continue;
                }

                bool exists = database.WeatherDataPoints.Any(p =>
                    p.StationName == point.StationName &&
                    p.Timestamp == point.Timestamp);

                if (exists)
                {
                    duplicates++;
                    continue;
                }

                database.WeatherDataPoints.Add(point);
                database.SaveChanges();
                imported++;
            }

            AnsiConsole.WriteLine(
                $"{imported} rader importerade, " +
                $"{rejected} rader förkastade på grund av fel, " +
                $"{duplicates} dubbletter överhoppade.");

            AnsiConsole.WriteLine($"Fellogg: {logPath}");
        }
        catch (IOException)
        {
            AnsiConsole.MarkupLine(
                "[red]Kunde inte läsa datafilen eller skriva felloggen.[/]");
        }
        catch (UnauthorizedAccessException)
        {
            AnsiConsole.MarkupLine(
                "[red]Programmet saknar behörighet till filerna.[/]");
        }
        catch (Exception ex) when (
            ex is DbUpdateException ||
            ex is Microsoft.Data.SqlClient.SqlException ||
            ex is InvalidOperationException)
        {
            AnsiConsole.MarkupLine(
                "[red]Databasfel. Importen avbröts.[/]");

            AnsiConsole.WriteLine(
                $"{imported} rader hann sparas. " +
                "Du kan försöka igen. Befintliga mätningar hoppas över.");
        }
    }

    private static bool TryParseLine(
        string line, out WeatherDataPoint point)
    {
        point = new WeatherDataPoint();
        string[] fields = line.Split('\t');

        if (fields.Length != 4)
        {
            return false;
        }

        string station = fields[0].Trim();

        if (string.IsNullOrWhiteSpace(station) || station.Length > 100)
        {
            return false;
        }

        if (!DateTime.TryParseExact(
                fields[1].Trim(),
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime timestamp))
        {
            return false;
        }

        if (!decimal.TryParse(
                fields[2].Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out decimal temperature) ||
            !decimal.TryParse(
                fields[3].Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out decimal humidity))
        {
            return false;
        }

        if (temperature < -90 || temperature > 60 ||
            humidity < 0 || humidity > 100)
        {
            return false;
        }

        point = new WeatherDataPoint
        {
            StationName = station,
            Timestamp = timestamp,
            Temperature = temperature,
            Humidity = humidity,
            DataSource = "CSV-File"
        };

        return true;
    }
}