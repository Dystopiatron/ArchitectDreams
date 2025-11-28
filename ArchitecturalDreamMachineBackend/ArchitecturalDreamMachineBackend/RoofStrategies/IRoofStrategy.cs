using ArchitecturalDreamMachineBackend.Models;

namespace ArchitecturalDreamMachineBackend.RoofStrategies
{
    /// <summary>
    /// Interface for roof calculation strategies
    /// Each strategy generates appropriate roof geometry for a building section
    /// </summary>
    public interface IRoofStrategy
    {
        /// <summary>
        /// Calculate roof geometry for a given roof section
        /// </summary>
        /// <param name="section">Roof section dimensions and position</param>
        /// <param name="roofPitch">Roof pitch (rise over 12)</param>
        /// <param name="overhang">Horizontal overhang beyond walls</param>
        /// <param name="hasParapet">Whether to include parapet walls (flat roofs)</param>
        /// <returns>Complete roof geometry with height information</returns>
        RoofGeometry CalculateRoof(
            RoofSection section,
            double roofPitch,
            double overhang,
            bool hasParapet = false);
    }
}
