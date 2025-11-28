using ArchitecturalDreamMachineBackend.Models;
using ArchitecturalDreamMachineBackend.LayoutStrategies;

namespace ArchitecturalDreamMachineBackend.Services
{
    /// <summary>
    /// Service to determine and calculate building layouts
    /// Selects appropriate layout strategy based on style and building shape
    /// </summary>
    public class LayoutService
    {
        private readonly ILogger<LayoutService> _logger;
        
        public LayoutService(ILogger<LayoutService> logger)
        {
            _logger = logger;
        }
        
        /// <summary>
        /// Determine appropriate layout and calculate building sections
        /// </summary>
        /// <param name="styleName">Architectural style (Victorian, Modern, etc.)</param>
        /// <param name="buildingShape">Requested shape (cube, l-shape, two-story, etc.)</param>
        /// <param name="footprintWidth">Base width</param>
        /// <param name="footprintDepth">Base depth</param>
        /// <param name="ceilingHeight">Height per floor</param>
        /// <param name="stories">Number of stories</param>
        /// <returns>Complete layout data</returns>
        public LayoutData DetermineLayout(
            string styleName,
            string buildingShape,
            double footprintWidth,
            double footprintDepth,
            double ceilingHeight,
            int stories)
        {
            _logger.LogInformation(
                "Determining layout: style={Style}, shape={Shape}, {Width}x{Depth}x{Stories}",
                styleName, buildingShape, footprintWidth, footprintDepth, stories);
            
            // Select strategy based on building shape
            ILayoutStrategy strategy = SelectStrategy(buildingShape, stories);
            
            var layout = strategy.CalculateLayout(
                footprintWidth,
                footprintDepth,
                ceilingHeight,
                stories);
            
            _logger.LogInformation(
                "Layout calculated: {Sections} sections, {Roofs} roof sections",
                layout.Sections.Count, layout.RoofSections.Count);
            
            return layout;
        }
        
        /// <summary>
        /// Select appropriate layout strategy
        /// </summary>
        private ILayoutStrategy SelectStrategy(string buildingShape, int stories)
        {
            // Normalize shape string
            string shape = (buildingShape ?? "").ToLower().Trim();
            
            return shape switch
            {
                "l-shape" => new LShapeLayoutStrategy(),
                "two-story" when stories >= 2 => new TwoStoryLayoutStrategy(),
                "split-level" => new SplitLevelLayoutStrategy(),
                "angled" => new AngledLayoutStrategy(),
                "cube" => new CubeLayoutStrategy(),
                _ => new CubeLayoutStrategy() // Default to simple cube
            };
        }
    }
}
