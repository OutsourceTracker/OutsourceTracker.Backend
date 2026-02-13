using Microsoft.EntityFrameworkCore;
using OutsourceTracker.Data.Converters;
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

        modelBuilder.Entity<Trailer>()
            .Property(e => e.Location)
            .HasConversion(new MapCoordinatesBinaryConverter())
            .HasColumnType("varbinary(24)");
    }
}
