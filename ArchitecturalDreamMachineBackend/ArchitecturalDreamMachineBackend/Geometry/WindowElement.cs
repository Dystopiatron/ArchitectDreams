namespace ArchitecturalDreamMachineBackend.Geometry;

/// <summary>
/// Represents a window with its relationship to a wall
/// Used for proper IFC export with IfcOpeningElement
/// </summary>
public class WindowElement
{
    /// <summary>
    /// Unique identifier for the window
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Display name for the window
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Room the window belongs to
    /// </summary>
    public string RoomName { get; set; } = string.Empty;
    
    /// <summary>
    /// Floor number (1-based)
    /// </summary>
    public int Floor { get; set; }
    
    /// <summary>
    /// Window width in feet
    /// </summary>
    public double Width { get; set; } = 3.0;
    
    /// <summary>
    /// Window height in feet
    /// </summary>
    public double Height { get; set; } = 4.0;
    
    /// <summary>
    /// Height from floor to bottom of window (sill height)
    /// </summary>
    public double SillHeight { get; set; } = 3.0;
    
    /// <summary>
    /// Global X position of window center
    /// </summary>
    public double X { get; set; }
    
    /// <summary>
    /// Global Y position of window center (vertical)
    /// </summary>
    public double Y { get; set; }
    
    /// <summary>
    /// Global Z position of window center
    /// </summary>
    public double Z { get; set; }
    
    /// <summary>
    /// Rotation around Y axis in radians (for side walls)
    /// </summary>
    public double RotationY { get; set; }
    
    // --- Wall relationship data ---
    
    /// <summary>
    /// Direction of the wall this window is on
    /// </summary>
    public WallDirection WallDirection { get; set; }
    
    /// <summary>
    /// ID of the WallSegment this window belongs to (populated by semantic model pipeline)
    /// </summary>
    public string WallSegmentId { get; set; } = string.Empty;
    
    /// <summary>
    /// Offset along the wall from its start point (in feet)
    /// </summary>
    public double OffsetAlongWall { get; set; }
    
    /// <summary>
    /// Wall thickness (for opening depth)
    /// </summary>
    public double WallThickness { get; set; } = 0.5;
    
    /// <summary>
    /// Material type for rendering
    /// </summary>
    public string MaterialType { get; set; } = "glass";
    
    /// <summary>
    /// Color hex code for rendering
    /// </summary>
    public string Color { get; set; } = "#b0e0e6";
}

/// <summary>
/// Direction of an exterior wall
/// </summary>
public enum WallDirection
{
    /// <summary>Facing positive Z (front)</summary>
    Front = 0,
    
    /// <summary>Facing negative Z (back)</summary>
    Back = 1,
    
    /// <summary>Facing negative X (left)</summary>
    Left = 2,
    
    /// <summary>Facing positive X (right)</summary>
    Right = 3
}
