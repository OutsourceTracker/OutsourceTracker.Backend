using Microsoft.EntityFrameworkCore;
using OutsourceTracker.Data.Converters;
using OutsourceTracker.Geolocation;
using OutsourceTracker.Models.Accounts;
using OutsourceTracker.Models.Trailers;

namespace OutsourceTracker.Data;

public class AppDataContext : DbContext
{
    public DbSet<Account> BusinessAccounts => Set<Account>();

    public DbSet<Trailer> Trailers => Set<Trailer>();

    public AppDataContext(DbContextOptions<AppDataContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Trailer>()
            .HasOne(e => e.Account)
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        ApplyConverters(modelBuilder);
    }

    private static void ApplyConverters(ModelBuilder modelBuilder)
    {
        var converter = new MapCoordinatesBinaryConverter();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITrackableEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var locationProperty = entityType.FindProperty("Location") ?? entityType.AddProperty("Location", typeof(MapCoordinates?));

            if (locationProperty?.ClrType == typeof(MapCoordinates?))
            {
                locationProperty.SetValueConverter(converter);
                locationProperty.SetColumnType("varbinary(24)");
                continue;
            }
        }
    }
}
