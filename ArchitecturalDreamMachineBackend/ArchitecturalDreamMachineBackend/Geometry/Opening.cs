namespace ArchitecturalDreamMachineBackend.Geometry;

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
