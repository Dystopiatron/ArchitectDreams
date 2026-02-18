using ArchitecturalDreamMachineBackend.Geometry;
using ArchitecturalDreamMachineBackend.Models;

namespace ArchitecturalDreamMachineBackend.Services;

/// <summary>
/// Interface for the design orchestration service
/// Enables dependency injection and easier unit testing
/// </summary>
public interface IDesignOrchestrationService
{
    /// <summary>
    /// Generate complete building geometry ready for frontend rendering
    /// </summary>
    /// <param name="parameters">House parameters from design generation</param>
    /// <returns>Complete building geometry with all sections, roofs, etc.</returns>
    BuildingGeometry GenerateCompleteGeometry(HouseParameters parameters);
}
