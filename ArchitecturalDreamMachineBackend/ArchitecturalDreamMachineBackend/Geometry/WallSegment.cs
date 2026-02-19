namespace ArchitecturalDreamMachineBackend.Geometry;

public enum WallType
{
    Exterior,
    Interior,
    Partition
}

public class WallSegment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public double StartX { get; set; }
    public double StartZ { get; set; }
    public double EndX { get; set; }
    public double EndZ { get; set; }
    public double BaseY { get; set; }
    public double Height { get; set; }
    public double Thickness { get; set; } = 0.5;
    public WallType Type { get; set; }
    public bool IsLoadBearing { get; set; }
    public int Floor { get; set; } = 1;
    public string MaterialType { get; set; } = "stucco";
    public string Color { get; set; } = "white";
    public List<Opening> Openings { get; set; } = new();
}
