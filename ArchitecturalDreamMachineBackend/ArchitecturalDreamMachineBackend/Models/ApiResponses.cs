using ArchitecturalDreamMachineBackend.Geometry;

namespace ArchitecturalDreamMachineBackend.Models;

/// <summary>
/// Response DTO for the Generate Design endpoint
/// Contains all data needed by the frontend to render the 3D house
/// </summary>
public class GenerateResponse
{
    /// <summary>
    /// Calculated house parameters based on lot size and style
    /// </summary>
    public HouseParameters HouseParameters { get; set; } = new();
    
    /// <summary>
    /// Legacy mesh data for backward compatibility
    /// </summary>
    public Mesh Mesh { get; set; } = new();
    
    /// <summary>
    /// Complete building geometry ready for Three.js rendering
    /// </summary>
    public BuildingGeometry Geometry { get; set; } = new();
    
    /// <summary>
    /// Database ID of the saved design
    /// </summary>
    public int DesignId { get; set; }
    
    /// <summary>
    /// Name of the matched style template
    /// </summary>
    public string StyleName { get; set; } = string.Empty;
}

/// <summary>
/// Standard error response DTO
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Human-readable error message
    /// </summary>
    public string Error { get; set; } = string.Empty;
    
    public ErrorResponse() { }
    
    public ErrorResponse(string error)
    {
        Error = error;
    }
}

/// <summary>
/// Response DTO for the GetAll Designs endpoint
/// </summary>
public class DesignsListResponse
{
    /// <summary>
    /// List of all saved designs
    /// </summary>
    public List<DesignSummary> Designs { get; set; } = new();
    
    /// <summary>
    /// Total count of designs
    /// </summary>
    public int Count { get; set; }
}

/// <summary>
/// Summary information about a saved design
/// </summary>
public class DesignSummary
{
    public int Id { get; set; }
    public double LotSize { get; set; }
    public string StyleKeywords { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
