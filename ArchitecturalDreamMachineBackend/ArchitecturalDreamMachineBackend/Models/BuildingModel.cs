using ArchitecturalDreamMachineBackend.Geometry;

namespace ArchitecturalDreamMachineBackend.Models;

public class Floor
{
    public int FloorNumber { get; set; }
    /// <summary>Elevation of the floor surface in feet (0-based)</summary>
    public double Elevation { get; set; }
    public List<string> RoomNames { get; set; } = new();
}

public class Slab
{
    /// <summary>"Floor" or "Ceiling"</summary>
    public string SlabType { get; set; } = string.Empty;
    public int FloorNumber { get; set; }
    public double Width { get; set; }
    public double Depth { get; set; }
    public double Thickness { get; set; } = 0.5;
}

public class RoofAssembly
{
    public string RoofType { get; set; } = "flat";
    public double Pitch { get; set; }
    public double PeakHeight { get; set; }
    public bool HasEaves { get; set; }
    public double EavesOverhang { get; set; }
}

public class BuildingModel
{
    public List<Floor> Floors { get; set; } = new();
    public List<WallSegment> ExteriorWalls { get; set; } = new();
    public List<WallSegment> InteriorWalls { get; set; } = new();
    public List<WindowElement> Windows { get; set; } = new();
    public List<DoorElement> Doors { get; set; } = new();
    public List<Slab> Slabs { get; set; } = new();
    public RoofAssembly? Roof { get; set; }
    public int Stories { get; set; }
    public double GrossFloorArea { get; set; }
}
