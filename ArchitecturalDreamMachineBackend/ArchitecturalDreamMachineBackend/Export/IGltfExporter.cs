using ArchitecturalDreamMachineBackend.Geometry;
using ArchitecturalDreamMachineBackend.Models;

namespace ArchitecturalDreamMachineBackend.Export;

/// <summary>
/// Interface for glTF/GLB export
/// </summary>
public interface IGltfExporter
{
    /// <summary>
    /// Export building design to GLB (binary glTF) format
    /// </summary>
    /// <param name="parameters">House parameters with room data</param>
    /// <param name="geometry">Generated building geometry</param>
    /// <param name="styleName">Style template name</param>
    /// <returns>GLB file as byte array</returns>
    byte[] ExportToGlb(HouseParameters parameters, BuildingGeometry geometry, string styleName);
}
