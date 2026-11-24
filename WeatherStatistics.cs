using Microsoft.EntityFrameworkCore;
using Spectre.Console;

namespace FailSafeWeatherMonitor;

public class WeatherStatistics
{
    public void Show()
    {
        try
        {
            using var database = new WeatherDbContext();

            var stations = database.WeatherDataPoints
                .Select(p => p.StationName)
                .Distinct()
                .OrderBy(name => name)
                .ToList();

            if (stations.Count == 0)
            {
                AnsiConsole.MarkupLine(
                    "[yellow]Det finns inga sparade mätningar ännu.[/]");
                return;
            }

            var stationTable = new Table();
            stationTable.AddColumn("Sparade mätstationer");

            foreach (string station in stations)
            {
                stationTable.AddRow(Markup.Escape(station));
            }

            AnsiConsole.Write(stationTable);

            string selectedStation = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[blue]Välj en mätstation[/]")
                    .UseConverter(name => Markup.Escape(name))
                    .AddChoices(stations));

            var measurements = database.WeatherDataPoints
                .AsNoTracking()
                .Where(p => p.StationName == selectedStation)
                .OrderBy(p => p.Timestamp)
                .ToList();

            if (measurements.Count == 0)
            {
                AnsiConsole.MarkupLine(
                    "[yellow]Stationen har inga mätningar kvar.[/]");
                return;
            }

            AnsiConsole.WriteLine(selectedStation);

            var measurementTable = new Table();
            measurementTable.AddColumn("Tid");
            measurementTable.AddColumn("Temperatur °C");
            measurementTable.AddColumn("Luftfuktighet %");
            measurementTable.AddColumn("Datakälla");

            foreach (var measurement in measurements)
            {
                measurementTable.AddRow(
                    measurement.Timestamp.ToString("yyyy-MM-dd HH:mm"),
                    measurement.Temperature.ToString("F3"),
                    measurement.Humidity.ToString("F1"),
                    Markup.Escape(measurement.DataSource));
            }

            AnsiConsole.Write(measurementTable);

            var summaryTable = new Table();
            summaryTable.AddColumn("Antal mätningar");
            summaryTable.AddColumn("Lägsta °C");
            summaryTable.AddColumn("Högsta °C");
            summaryTable.AddColumn("Medel °C");

            summaryTable.AddRow(
                measurements.Count.ToString(),
                measurements.Min(p => p.Temperature).ToString("F3"),
                measurements.Max(p => p.Temperature).ToString("F3"),
                measurements.Average(p => p.Temperature).ToString("F3"));

            AnsiConsole.Write(summaryTable);
        }
        catch (Exception ex) when (
            ex is Microsoft.Data.SqlClient.SqlException ||
            ex is InvalidOperationException)
        {
            AnsiConsole.MarkupLine(
                "[red]Kunde inte läsa statistiken. Kontrollera databasanslutningen.[/]");
        }
    }
}