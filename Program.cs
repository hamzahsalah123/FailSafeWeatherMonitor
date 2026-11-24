using FailSafeWeatherMonitor;
using Spectre.Console;

bool running = true;

while (running)
{
    string choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("[blue]FailSafe WeatherMonitor[/]")
            .AddChoices(
                "Importera historisk data från fil",
                "Hämta live-data från API",
                "Visa väderstatistik",
                "Avsluta"));

    switch (choice)
    {
        case "Importera historisk data från fil":
            var importer = new WeatherFileImporter();
            importer.Import();
            break;
        case "Hämta live-data från API":
            var apiClient = new WeatherApiClient();
            apiClient.FetchAndSave();
            break;
        case "Visa väderstatistik":
            var statistics = new WeatherStatistics();
            statistics.Show();
            break;
        case "Avsluta":
            running = false;
            break;

        default:
            AnsiConsole.MarkupLine(
                "[yellow]Funktionen lägger vi till senare.[/]");
            break;
    }

    if (running)
    {
        AnsiConsole.WriteLine("Tryck på valfri tangent för att fortsätta.");
        Console.ReadKey(true);
        AnsiConsole.Clear();
    }
}