using ArchitecturalDreamMachineBackend.Geometry;
using ArchitecturalDreamMachineBackend.Models;

namespace ArchitecturalDreamMachineBackend.Services;

/// <summary>
/// Interface for window generation
/// </summary>
public interface IWindowService
{
    /// <summary>
    /// Generate window geometries for all rooms
    /// </summary>
    /// <param name="windowStyle">Style-specific window type: "small", "large", or "ornate"</param>
    List<GeometryData> GenerateWindows(
        List<Room> rooms,
        double windowToWallRatio,
        double ceilingHeight,
        double footprintWidth,
        double footprintDepth,
        string buildingShape = "rectangular",
        string windowStyle = "standard");

    /// <summary>
    /// Generate window elements with wall relationships for BIM export
    /// </summary>
    /// <param name="windowStyle">Style-specific window type: "small", "large", or "ornate"</param>
    List<WindowElement> GenerateWindowElements(
        List<Room> rooms,
        double windowToWallRatio,
        double ceilingHeight,
        double footprintWidth,
        double footprintDepth,
        string buildingShape = "rectangular",
        string windowStyle = "standard");

    /// <summary>
    /// Generate window elements linked to typed exterior wall segments
    /// </summary>
    List<WindowElement> GenerateLinkedWindowElements(
        List<Room> rooms,
        double windowToWallRatio,
        double ceilingHeight,
        double footprintWidth,
        double footprintDepth,
        List<WallSegment> exteriorWalls,
        string buildingShape = "rectangular",
        string windowStyle = "standard");
}
