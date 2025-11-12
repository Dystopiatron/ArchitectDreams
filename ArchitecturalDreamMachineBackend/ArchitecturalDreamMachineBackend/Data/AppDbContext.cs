using Microsoft.EntityFrameworkCore;

namespace ArchitecturalDreamMachineBackend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Design> Designs { get; set; }
    public DbSet<StyleTemplate> StyleTemplates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seed StyleTemplates
        modelBuilder.Entity<StyleTemplate>().HasData(
            new StyleTemplate
            {
                Id = 1,
                Name = "Brutalist",
                RoofType = "flat",
                WindowStyle = "small",
                RoomCount = 4,
                Color = "gray",
                Texture = "concrete"
            },
            new StyleTemplate
            {
                Id = 2,
                Name = "Victorian",
                RoofType = "gabled",
                WindowStyle = "ornate",
                RoomCount = 6,
                Color = "cream",
                Texture = "wood"
            },
            new StyleTemplate
            {
                Id = 3,
                Name = "Modern",
                RoofType = "flat",
                WindowStyle = "large",
                RoomCount = 5,
                Color = "white",
                Texture = "glass"
            }
        );
    }
}
