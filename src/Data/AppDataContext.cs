using Microsoft.EntityFrameworkCore;
using OutsourceTracker.Data.Converters;
using OutsourceTracker.Geolocation;
using OutsourceTracker.Models.Accounts;
using OutsourceTracker.Models.Trailers;
using OutsourceTracker.Models.Zones;

namespace OutsourceTracker.Data;

public class AppDataContext : DbContext
{
    public DbSet<Account> BusinessAccounts => Set<Account>();

    public DbSet<Trailer> Trailers => Set<Trailer>();

    public DbSet<Zone> Zones => Set<Zone>();

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

        modelBuilder.Entity<Zone>(ent =>
        {
            ent.Property(z => z.Boundry)
            .HasConversion(new PolygonBinaryConverter());
        });

        ApplyConverters(modelBuilder);
    }

    private static void ApplyConverters(ModelBuilder modelBuilder)
    {

        var converter = new Vector2BinaryConverter();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITrackableEntity<Guid>).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var locationProperty = entityType.FindProperty("Location") ?? entityType.AddProperty("Location", typeof(Vector2?));

            if (locationProperty?.ClrType == typeof(Vector2?))
            {
                locationProperty.SetValueConverter(converter);
                continue;
            }
        }
    }
}
