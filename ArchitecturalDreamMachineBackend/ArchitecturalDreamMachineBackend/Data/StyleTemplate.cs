namespace ArchitecturalDreamMachineBackend.Data;

public class StyleTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RoofType { get; set; } = string.Empty;
    public string WindowStyle { get; set; } = string.Empty;
    public int RoomCount { get; set; }
    public string Color { get; set; } = string.Empty;
    public string Texture { get; set; } = string.Empty;
    
    // Architectural parameters
    public double TypicalCeilingHeight { get; set; } = 9.0; // 8, 9, 10, 12 ft
    public int TypicalStories { get; set; } = 1; // 1, 2, 3
    public string BuildingShape { get; set; } = "rectangular"; // "rectangular", "l-shape", "u-shape"
    public double WindowToWallRatio { get; set; } = 0.15; // 0.10-0.30 (10%-30%)
    public string FoundationType { get; set; } = "slab"; // "slab", "crawlspace", "basement"
    public string ExteriorMaterial { get; set; } = "stucco"; // "stucco", "siding", "brick", "concrete"
    public double RoofPitch { get; set; } = 6.0; // 4/12, 6/12, 8/12, 12/12 (0 for flat)
    public bool HasParapet { get; set; } = false; // Modern flat roofs
    public bool HasEaves { get; set; } = true; // Overhanging eaves
    public double EavesOverhang { get; set; } = 1.5; // 1.0, 1.5, 2.0 ft
}
