using HazardDashboard.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace HazardDashboard.Api.Data;

public sealed class HazardDbContext(DbContextOptions<HazardDbContext> options) : DbContext(options)
{
    public DbSet<Volcano> Volcanoes => Set<Volcano>();
    public DbSet<SeismicEvent> SeismicEvents => Set<SeismicEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.Entity<Volcano>(entity =>
        {
            // Geography models earth coordinates and lets PostGIS distance functions use metres.
            // Geometry is faster/more flexible for planar projections, but would require care
            // around projection choice for real-world radius queries.
            entity.Property(v => v.Location).HasColumnType("geography (point, 4326)");
            entity.Property(v => v.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(v => v.Location).HasMethod("gist");
        });

        modelBuilder.Entity<SeismicEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Location).HasColumnType("geography (point, 4326)");
            entity.HasIndex(e => e.Location).HasMethod("gist");
            entity.HasIndex(e => e.OccurredAt);
        });
    }
}
