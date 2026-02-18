using ArchitecturalDreamMachineBackend.Geometry;
using ArchitecturalDreamMachineBackend.Models;

namespace ArchitecturalDreamMachineBackend.Export;

/// <summary>
/// Interface for IFC (Industry Foundation Classes) export
/// </summary>
public interface IIfcExporter
{
    /// <summary>
    /// Export building design to IFC format
    /// </summary>
    /// <param name="parameters">House parameters with room data</param>
    /// <param name="geometry">Generated building geometry</param>
    /// <param name="styleName">Style template name</param>
    /// <returns>IFC file as byte array</returns>
    byte[] ExportToIfc(HouseParameters parameters, BuildingGeometry geometry, string styleName);
}
