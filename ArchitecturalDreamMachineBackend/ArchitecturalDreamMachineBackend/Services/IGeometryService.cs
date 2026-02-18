using ArchitecturalDreamMachineBackend.Models;

namespace ArchitecturalDreamMachineBackend.Services;

/// <summary>
/// Interface for geometry generation service
/// Provides methods for creating geometric primitives for Three.js rendering
/// </summary>
public interface IGeometryService
{
    /// <summary>
    /// Create a box geometry (building section, floor platform, etc.)
    /// </summary>
    /// <param name="width">Width (X dimension)</param>
    /// <param name="height">Height (Y dimension)</param>
    /// <param name="depth">Depth (Z dimension)</param>
    /// <param name="x">X position offset</param>
    /// <param name="y">Y position offset</param>
    /// <param name="z">Z position offset</param>
    /// <param name="materialType">Material type for rendering</param>
    /// <param name="color">Color name or hex</param>
    /// <returns>Complete geometry ready for Three.js</returns>
    GeometryData CreateBox(
        double width,
        double height,
        double depth,
        double x,
        double y,
        double z,
        string materialType = "stucco",
        string color = "white");

    /// <summary>
    /// Create a traditional gabled roof geometry
    /// </summary>
    /// <param name="width">Building width (roof spans this dimension)</param>
    /// <param name="depth">Building depth (ridge runs this direction)</param>
    /// <param name="roofPitch">Roof pitch as rise over 12 (e.g., 8.0 for 8:12)</param>
    /// <param name="overhang">Horizontal overhang beyond walls</param>
    /// <returns>Gabled roof geometry</returns>
    GeometryData CreateGabledRoof(
        double width,
        double depth,
        double roofPitch,
        double overhang);

    /// <summary>
    /// Create a flat roof geometry (thin box)
    /// </summary>
    /// <param name="width">Building width</param>
    /// <param name="depth">Building depth</param>
    /// <param name="overhang">Horizontal overhang</param>
    /// <param name="thickness">Roof thickness</param>
    /// <returns>Flat roof geometry</returns>
    GeometryData CreateFlatRoof(
        double width,
        double depth,
        double overhang,
        double thickness = 0.75);

    /// <summary>
    /// Create parapet wall geometries for flat roofs
    /// </summary>
    /// <param name="width">Building width</param>
    /// <param name="depth">Building depth</param>
    /// <param name="overhang">Roof overhang</param>
    /// <param name="parapetHeight">Height of parapet walls</param>
    /// <param name="parapetThickness">Thickness of parapet walls</param>
    /// <returns>List of 4 parapet geometries (front, back, left, right)</returns>
    List<GeometryData> CreateParapetWalls(
        double width,
        double depth,
        double overhang,
        double parapetHeight = 2.5,
        double parapetThickness = 0.5);

    /// <summary>
    /// Create a simple quad for windows or doors
    /// </summary>
    /// <param name="width">Quad width</param>
    /// <param name="height">Quad height</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="z">Z position</param>
    /// <param name="materialType">Material type</param>
    /// <param name="color">Color</param>
    /// <returns>Quad geometry</returns>
    GeometryData CreateQuad(
        double width,
        double height,
        double x,
        double y,
        double z,
        string materialType = "glass",
        string color = "#87ceeb");
}
