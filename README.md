# FailSafe WeatherMonitor

En C#-konsolapplikation som importerar väderdata från fil, hämtar aktuellt väder från Open-Meteo och visar statistik från en SQL Server-databas.

Projektet är byggt för G-kraven med synkron programstruktur.

## Teknik

- C# och .NET 8
- Entity Framework Core 8 med SQL Server
- EF Core Code First och Migrations
- Spectre.Console för meny och tabeller
- HttpClient och System.Text.Json för API-anrop

## Starta programmet

1. Installera Visual Studio med stöd för .NET 8 och SQL Server LocalDB.
2. Öppna projektets .sln- eller .csproj-fil.
3. Låt NuGet återställa projektets paket.
4. Öppna Tools → NuGet Package Manager → Package Manager Console.
5. Kontrollera att FailSafeWeatherMonitor är valt som Default project.
6. Kör `Update-Database` för att skapa databasen med projektets migrations.
7. Starta via Debug → Start Without Debugging.

Anslutningssträngen finns i appsettings.json och använder
(localdb)\MSSQLLocalDB med Windows-autentisering.

Både appsettings.json och weather_data.csv ska ha
Copy to Output Directory inställt på Copy if newer.

## Funktioner

### Importera historisk data från fil

Programmet läser weather_data.csv, som innehåller 200 mätningar.
Trots filändelsen är kolumnerna separerade med tabbar.

Varje rad kontrolleras med TryParse-metoder och rimlighetskontroller.
Stationens namn måste vara ifyllt, temperaturen ska ligga mellan
−90 och 60 °C och luftfuktigheten mellan 0 och 100 procent.

G-versionen förkastar tal med decimalkomma. Förkastade rader skrivs
oförändrade till bad_data.log i programmets körmapp.
Loggfilen skrivs över vid varje import.

### Hämta live-data från API

Programmet hämtar aktuell temperatur och luftfuktighet för Göteborg
från Open-Meteo och sparar mätningen med DataSource satt till API.
API-tiden hämtas som svensk lokal tid.

Nätverksfel, timeout och felaktiga JSON-svar hanteras med try-catch.
Databasfel visas som meddelanden för användaren.

### Visa väderstatistik

Användaren får se alla unika stationer och kan välja en med
piltangenterna. Stationens mätningar visas i tidsordning.

Antal mätningar samt lägsta, högsta och genomsnittliga temperatur
visas i en tabell. Beräkningarna använder LINQ.

## Databas och dubbletter

WeatherDataPoint innehåller Id, StationName, Timestamp,
Temperature, Humidity och DataSource.

Före sparning kontrollerar programmet om samma StationName och
Timestamp redan finns. Ett unikt sammansatt index skyddar även
databasen mot dubbletter.

Migrations-mappen innehåller schemahistoriken. Den andra migrationen
ändrar temperaturkolumnen till tre decimaler för att bevara filens värden.

## Genomförda kontroller

- Filimporten ger 14 förkastade rader i bad_data.log.
- Vid upprepad import hoppas de 186 befintliga mätningarna över.
- Ett API-anrop har visat temperatur och luftfuktighet för Göteborg.
- Statistikvyn visar sju unika stationer.
- Stockholm_Centrum visar 16 mätningar, lägsta −2,499 °C,
  högsta 14,500 °C och medeltemperaturen 5,796 °C.

## Konfiguration och säkerhet

appsettings.json ingår enligt uppgiftens instruktioner.
Den lokala anslutningssträngen innehåller inget lösenord.

I en produktionsmiljö bör hemligheter hanteras genom exempelvis
miljövariabler eller Azure Key Vault.

## Datakällor

Historiska mätningar kommer från lärarens weather_data.csv.

Aktuellt väder hämtas från Open-Meteo.
Dokumentation finns på https://open-meteo.com/en/docs.