using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FailSafeWeatherMonitor;

public class WeatherDbContext : DbContext
{
    public DbSet<WeatherDataPoint> WeatherDataPoints =>
        Set<WeatherDataPoint>();

    protected override void OnConfiguring(
        DbContextOptionsBuilder optionsBuilder)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var connectionString =
            configuration.GetConnectionString("WeatherDatabase")
            ?? throw new InvalidOperationException(
                "Anslutningssträngen WeatherDatabase saknas.");

        optionsBuilder.UseSqlServer(connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var weather = modelBuilder.Entity<WeatherDataPoint>();

        weather.Property(p => p.StationName)
            .HasMaxLength(100)
            .IsRequired();

        weather.Property(p => p.DataSource)
            .HasMaxLength(20)
            .IsRequired();

        weather.Property(p => p.Temperature)
            .HasPrecision(6, 3);

        weather.Property(p => p.Humidity)
            .HasPrecision(5, 2);

        weather.HasIndex(p => new { p.StationName, p.Timestamp })
            .IsUnique();
    }
}