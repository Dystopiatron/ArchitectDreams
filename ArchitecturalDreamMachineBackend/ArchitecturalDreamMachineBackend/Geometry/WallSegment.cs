namespace ArchitecturalDreamMachineBackend.Geometry;

public enum WallType
{
    Exterior,
    Interior,
    Partition
}

public enum OpeningType
{
    Window,
    Door
}

/// <summary>
/// Represents a window or door opening within a wall segment
/// </summary>
public class Opening
{
    public OpeningType Type { get; set; }
    /// <summary>ID of the linked WindowElement or DoorElement</summary>
    public string ElementId { get; set; } = string.Empty;
    /// <summary>Distance in feet from the wall's start point to the opening centre</summary>
    public double PositionAlongWall { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    /// <summary>Height from floor to bottom of opening (0 for doors)</summary>
    public double SillHeight { get; set; }
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
