using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OutsourceTracker.Authentication;
using OutsourceTracker.BusinessUnit.Accounts;
using OutsourceTracker.BusinessUnit.Divisions;
using OutsourceTracker.Data.Converters;
using OutsourceTracker.Equipment.Trailers;
using OutsourceTracker.Geolocation;
using OutsourceTracker.Models.Zones;
using System.Text.Json;

namespace OutsourceTracker.Data;

public class AppDataContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public DbSet<OrganizationalUnitDbModel> BusinessUnits => Set<OrganizationalUnitDbModel>();

    public DbSet<AccountDbModel> BusinessAccounts => Set<AccountDbModel>();

    public DbSet<TrailerDbModel> Trailers => Set<TrailerDbModel>();

    public DbSet<Zone> Zones => Set<Zone>();

    public AppDataContext(DbContextOptions<AppDataContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Zone>(ent =>
        {
            ent.Property(z => z.Boundry)
            .HasConversion(new PolygonBinaryConverter());

            ent.Property(z => z.EntryPoints)
            .HasConversion(
                v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                v => JsonSerializer.Deserialize<ICollection<Vector2>>(v, new JsonSerializerOptions())
                !);

            ent.Property(z => z.ExitPoints)
            .HasConversion(
                v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                v => JsonSerializer.Deserialize<ICollection<Vector2>>(v, new JsonSerializerOptions())
                !);

            ent.Property(z => z.DockPoints)
            .HasConversion(
                v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                v => JsonSerializer.Deserialize<ICollection<Vector2>>(v, new JsonSerializerOptions())
                !);
        });

        ApplyConverters(modelBuilder);
        base.OnModelCreating(modelBuilder);
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
