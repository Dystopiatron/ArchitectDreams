using ArchitecturalDreamMachineBackend.Models;

namespace ArchitecturalDreamMachineBackend.LayoutStrategies
{
    /// <summary>
    /// Interface for building layout calculation strategies
    /// Each strategy determines how to arrange building sections and roofs
    /// </summary>
    public interface ILayoutStrategy
    {
        /// <summary>
        /// Calculate the complete layout for a building
        /// </summary>
        /// <param name="footprintWidth">Base footprint width</param>
        /// <param name="footprintDepth">Base footprint depth</param>
        /// <param name="ceilingHeight">Height per story</param>
        /// <param name="stories">Number of stories</param>
        /// <returns>Complete layout data with sections and roof placements</returns>
        LayoutData CalculateLayout(
            double footprintWidth,
            double footprintDepth,
            double ceilingHeight,
            int stories);
    }
}
