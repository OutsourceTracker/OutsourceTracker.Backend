using Microsoft.EntityFrameworkCore;
using OutsourceTracker.Models.Trailers;

namespace OutsourceTracker.Data;

public class AppDataContext : DbContext
{
    public DbSet<CommercialTrailer> Trailers => Set<CommercialTrailer>();

    public AppDataContext(DbContextOptions<AppDataContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CommercialTrailer>(x =>
        {
            x.HasIndex(n => n.FullName).IsUnique();
        });
    }
}
