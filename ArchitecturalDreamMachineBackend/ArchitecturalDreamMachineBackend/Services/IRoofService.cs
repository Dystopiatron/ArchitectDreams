using ArchitecturalDreamMachineBackend.Models;

namespace ArchitecturalDreamMachineBackend.Services;

/// <summary>
/// Interface for roof calculation service
/// Calculates roof geometries based on roof type and building sections
/// </summary>
public interface IRoofService
{
    /// <summary>
    /// Calculate roof geometries for all roof sections
    /// </summary>
    /// <param name="sections">List of roof sections to cover</param>
    /// <param name="roofType">Type of roof (gabled, flat, etc.)</param>
    /// <param name="roofPitch">Roof pitch (rise over 12)</param>
    /// <param name="overhang">Horizontal overhang</param>
    /// <param name="hasParapet">Whether to include parapet walls</param>
    /// <returns>List of roof geometries</returns>
    List<RoofGeometry> CalculateRoofs(
        List<RoofSection> sections,
        string roofType,
        double roofPitch,
        double overhang,
        bool hasParapet);

    /// <summary>
    /// Calculate a single roof
    /// </summary>
    /// <param name="section">Roof section to cover</param>
    /// <param name="roofType">Type of roof</param>
    /// <param name="roofPitch">Roof pitch</param>
    /// <param name="overhang">Overhang amount</param>
    /// <param name="hasParapet">Whether to include parapet</param>
    /// <returns>Roof geometry</returns>
    RoofGeometry CalculateRoof(
        RoofSection section,
        string roofType,
        double roofPitch,
        double overhang,
        bool hasParapet);
}
