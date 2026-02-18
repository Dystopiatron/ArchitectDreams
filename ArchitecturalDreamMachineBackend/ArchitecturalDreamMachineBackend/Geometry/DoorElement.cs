namespace ArchitecturalDreamMachineBackend.Geometry;

/// <summary>
/// Represents a door with its relationship to walls and rooms
/// Used for proper IFC export with IfcOpeningElement
/// </summary>
public class DoorElement
{
    /// <summary>
    /// Unique identifier for the door
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Display name for the door
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Room on one side of the door
    /// </summary>
    public string FromRoomName { get; set; } = string.Empty;
    
    /// <summary>
    /// Room on the other side of the door
    /// </summary>
    public string ToRoomName { get; set; } = string.Empty;
    
    /// <summary>
    /// Floor number (1-based)
    /// </summary>
    public int Floor { get; set; }
    
    /// <summary>
    /// Door width in feet
    /// </summary>
    public double Width { get; set; } = 3.0;
    
    /// <summary>
    /// Door height in feet
    /// </summary>
    public double Height { get; set; } = 7.0;
    
    /// <summary>
    /// Global X position of door center
    /// </summary>
    public double X { get; set; }
    
    /// <summary>
    /// Global Y position of door center (vertical)
    /// </summary>
    public double Y { get; set; }
    
    /// <summary>
    /// Global Z position of door center
    /// </summary>
    public double Z { get; set; }
    
    /// <summary>
    /// Rotation around Y axis in radians
    /// </summary>
    public double RotationY { get; set; }
    
    /// <summary>
    /// Whether the door is on an exterior wall
    /// </summary>
    public bool IsExterior { get; set; } = false;
    
    /// <summary>
    /// Wall orientation (horizontal runs along X, vertical runs along Z)
    /// </summary>
    public DoorWallOrientation WallOrientation { get; set; }
    
    /// <summary>
    /// Wall thickness (for opening depth)
    /// </summary>
    public double WallThickness { get; set; } = 0.5;
    
    /// <summary>
    /// Material type for rendering
    /// </summary>
    public string MaterialType { get; set; } = "wood";
    
    /// <summary>
    /// Color hex code for rendering
    /// </summary>
    public string Color { get; set; } = "#8B4513"; // Saddle brown
    
    /// <summary>
    /// Door operation type for IFC
    /// </summary>
    public DoorOperationType OperationType { get; set; } = DoorOperationType.SingleSwingLeft;

    /// <summary>
    /// ID of the host WallSegment (populated by semantic model pipeline)
    /// </summary>
    public string WallSegmentId { get; set; } = string.Empty;
}

/// <summary>
/// Wall orientation for door placement
/// </summary>
public enum DoorWallOrientation
{
    /// <summary>Wall runs along X axis</summary>
    Horizontal = 0,
    
    /// <summary>Wall runs along Z axis</summary>
    Vertical = 1
}

/// <summary>
/// Door operation type (swing direction)
/// </summary>
public enum DoorOperationType
{
    SingleSwingLeft,
    SingleSwingRight,
    DoubleDoorSingle,
    Sliding,
    Folding
}
