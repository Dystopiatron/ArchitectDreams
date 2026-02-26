namespace ArchitecturalDreamMachineBackend.Geometry;

/// <summary>
/// A single opening (window or door) expressed in face-local 2D coordinates.
/// OffsetX is horizontal distance from the face centre; OffsetY is the vertical
/// centre of the opening measured from the section's base Y.
/// </summary>
public class WallOpeningData
{
    /// <summary>"window" or "door"</summary>
    public string Type { get; set; } = "window";
    /// <summary>Horizontal offset from face centre (face-local X axis)</summary>
    public double OffsetX { get; set; }
    /// <summary>Vertical centre from section base Y (face-local Y axis)</summary>
    public double OffsetY { get; set; }
    public double Width  { get; set; }
    public double Height { get; set; }
}

/// <summary>
/// One exterior wall face panel for a building section.
/// Position (X, Y, Z): X/Z = world face centre; Y = section base (floor level).
/// RotationY aligns the flat XY-plane ShapeGeometry to the correct wall face.
/// </summary>
public class WallFaceData
{
    /// <summary>World X of face centre</summary>
    public double X { get; set; }
    /// <summary>World Y of section base (floor level of the storey)</summary>
    public double Y { get; set; }
    /// <summary>World Z of face centre</summary>
    public double Z { get; set; }
    public double Width     { get; set; }
    public double Height    { get; set; }
    /// <summary>
    /// Y-axis rotation in radians:
    ///   0     = Front  (faces +Z)
    ///   π     = Back   (faces -Z)
    ///  -π/2   = Right  (faces +X)
    ///   π/2   = Left   (faces -X)
    /// </summary>
    public double RotationY { get; set; }
    public string MaterialType { get; set; } = "stucco";
    public string Color        { get; set; } = "white";
    public List<WallOpeningData> Openings { get; set; } = new();
}

/// <summary>
/// Result of wall face generation: face panels + the set of window IDs
/// that were successfully placed on visible (non-overlapped) face areas.
/// </summary>
public class WallFaceResult
{
    public List<WallFaceData> Faces { get; set; } = new();
    public HashSet<string> PlacedWindowIds { get; set; } = new();
}
