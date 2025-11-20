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
                Texture = "concrete",
                TypicalCeilingHeight = 12.0, // Dramatic height
                TypicalStories = 1,
                BuildingShape = "rectangular",
                WindowToWallRatio = 0.10, // Minimal glass
                FoundationType = "slab",
                ExteriorMaterial = "concrete",
                RoofPitch = 0, // Flat
                HasParapet = true,
                HasEaves = false,
                EavesOverhang = 0
            },
            new StyleTemplate
            {
                Id = 2,
                Name = "Victorian",
                RoofType = "gabled",
                WindowStyle = "ornate",
                RoomCount = 6,
                Color = "cream",
                Texture = "wood",
                TypicalCeilingHeight = 9.0,
                TypicalStories = 2,
                BuildingShape = "l-shape",
                WindowToWallRatio = 0.20, // Many smaller windows
                FoundationType = "crawlspace",
                ExteriorMaterial = "wood siding",
                RoofPitch = 8.0, // 8:12 steep
                HasParapet = false,
                HasEaves = true,
                EavesOverhang = 2.0
            },
            new StyleTemplate
            {
                Id = 3,
                Name = "Modern",
                RoofType = "flat",
                WindowStyle = "large",
                RoomCount = 5,
                Color = "white",
                Texture = "glass",
                TypicalCeilingHeight = 10.0,
                TypicalStories = 2,
                BuildingShape = "rectangular",
                WindowToWallRatio = 0.30, // Large windows
                FoundationType = "slab",
                ExteriorMaterial = "stucco",
                RoofPitch = 0, // Flat
                HasParapet = true,
                HasEaves = false,
                EavesOverhang = 0
            }
        );
    }
}
