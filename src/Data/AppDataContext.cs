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

    // We do NOT expose Identity's UserPasskeys DbSet directly here.
    // This avoids the "not included in the model" error because .AddWebAuthn() is not registered.
    // Instead, we store passkey credentials + friendly names in our own PasskeyMetadata table
    // (this is the recommended approach when using JWT auth + full Fido2 control).

    // Our table for passkeys (stores credential data + friendly name)
    public DbSet<PasskeyMetadata> PasskeyMetadata => Set<PasskeyMetadata>();

    public AppDataContext(DbContextOptions<AppDataContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Important for Identity

        modelBuilder.Entity<PasskeyMetadata>(ent =>
        {
            ent.HasIndex(x => x.CredentialId).IsUnique();
            ent.HasIndex(x => x.UserId);

            ent.HasOne(x => x.User)
               .WithMany()
               .HasForeignKey(x => x.UserId)
               .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Zone>(ent =>
        {
            ent.Property(z => z.Boundry)
            .HasConversion(new PolygonBinaryConverter());

            ent.Property(z => z.EntryPoints)
            .HasConversion(
                v => JsonSerializer.Serialize(v ?? new List<Vector2>(), new JsonSerializerOptions()),
                v => string.IsNullOrWhiteSpace(v)
                    ? new List<Vector2>()
                    : JsonSerializer.Deserialize<ICollection<Vector2>>(v, new JsonSerializerOptions()) ?? new List<Vector2>());

            ent.Property(z => z.ExitPoints)
            .HasConversion(
                v => JsonSerializer.Serialize(v ?? new List<Vector2>(), new JsonSerializerOptions()),
                v => string.IsNullOrWhiteSpace(v)
                    ? new List<Vector2>()
                    : JsonSerializer.Deserialize<ICollection<Vector2>>(v, new JsonSerializerOptions()) ?? new List<Vector2>());

            ent.Property(z => z.DockPoints)
            .HasConversion(
                v => JsonSerializer.Serialize(v ?? new List<Vector2>(), new JsonSerializerOptions()),
                v => string.IsNullOrWhiteSpace(v)
                    ? new List<Vector2>()
                    : JsonSerializer.Deserialize<ICollection<Vector2>>(v, new JsonSerializerOptions()) ?? new List<Vector2>());

            ent.Property(z => z.TrailerPools)
            .HasConversion(
                v => JsonSerializer.Serialize(v ?? new List<Vector2>(), new JsonSerializerOptions()),
                v => string.IsNullOrWhiteSpace(v)
                    ? new List<Vector2>()
                    : JsonSerializer.Deserialize<ICollection<Vector2>>(v, new JsonSerializerOptions()) ?? new List<Vector2>());
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
