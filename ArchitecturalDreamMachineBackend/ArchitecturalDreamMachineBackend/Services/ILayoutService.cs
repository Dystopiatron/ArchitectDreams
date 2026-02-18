using ArchitecturalDreamMachineBackend.Geometry;
using ArchitecturalDreamMachineBackend.Models;

namespace ArchitecturalDreamMachineBackend.Services;

/// <summary>
/// Interface for building layout service
/// Determines and calculates building sections based on shape and style
/// </summary>
public interface ILayoutService
{
    /// <summary>
    /// Determine appropriate layout and calculate building sections
    /// </summary>
    LayoutData DetermineLayout(
        string styleName,
        string buildingShape,
        double footprintWidth,
        double footprintDepth,
        double ceilingHeight,
        int stories);

    /// <summary>
    /// Decompose layout sections into typed exterior wall segments
    /// </summary>
    List<WallSegment> GenerateExteriorWalls(
        LayoutData layout,
        double ceilingHeight,
        int stories,
        string material,
        string color);
}
