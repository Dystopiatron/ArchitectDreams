using ArchitecturalDreamMachineBackend.Geometry;
using ArchitecturalDreamMachineBackend.Models;
using ArchitecturalDreamMachineBackend.LayoutStrategies;

namespace ArchitecturalDreamMachineBackend.Services
{
    /// <summary>
    /// Service to determine and calculate building layouts
    /// Selects appropriate layout strategy based on style and building shape
    /// </summary>
    public class LayoutService : ILayoutService
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
        /// Decompose layout sections into typed exterior wall segments.
        /// Each section box produces 4 wall faces (front, back, left, right).
        /// </summary>
        public List<WallSegment> GenerateExteriorWalls(
            LayoutData layout,
            double ceilingHeight,
            int stories,
            string material,
            string color)
        {
            var walls = new List<WallSegment>();
            const double wallThickness = 0.5;

            foreach (var section in layout.Sections)
            {
                double halfW = section.Width / 2.0;
                double halfD = section.Depth / 2.0;
                double baseY = section.Y - section.Height / 2.0;

                // Front wall (+Z face)
                walls.Add(new WallSegment
                {
                    Name = $"Exterior_Front_F{section.Floor}",
                    StartX = section.X - halfW,
                    StartZ = section.Z + halfD,
                    EndX = section.X + halfW,
                    EndZ = section.Z + halfD,
                    BaseY = baseY,
                    Height = section.Height,
                    Thickness = wallThickness,
                    Type = WallType.Exterior,
                    IsLoadBearing = true,
                    Floor = section.Floor,
                    MaterialType = material,
                    Color = color
                });

                // Back wall (-Z face)
                walls.Add(new WallSegment
                {
                    Name = $"Exterior_Back_F{section.Floor}",
                    StartX = section.X + halfW,
                    StartZ = section.Z - halfD,
                    EndX = section.X - halfW,
                    EndZ = section.Z - halfD,
                    BaseY = baseY,
                    Height = section.Height,
                    Thickness = wallThickness,
                    Type = WallType.Exterior,
                    IsLoadBearing = true,
                    Floor = section.Floor,
                    MaterialType = material,
                    Color = color
                });

                // Left wall (-X face)
                walls.Add(new WallSegment
                {
                    Name = $"Exterior_Left_F{section.Floor}",
                    StartX = section.X - halfW,
                    StartZ = section.Z - halfD,
                    EndX = section.X - halfW,
                    EndZ = section.Z + halfD,
                    BaseY = baseY,
                    Height = section.Height,
                    Thickness = wallThickness,
                    Type = WallType.Exterior,
                    IsLoadBearing = true,
                    Floor = section.Floor,
                    MaterialType = material,
                    Color = color
                });

                // Right wall (+X face)
                walls.Add(new WallSegment
                {
                    Name = $"Exterior_Right_F{section.Floor}",
                    StartX = section.X + halfW,
                    StartZ = section.Z + halfD,
                    EndX = section.X + halfW,
                    EndZ = section.Z - halfD,
                    BaseY = baseY,
                    Height = section.Height,
                    Thickness = wallThickness,
                    Type = WallType.Exterior,
                    IsLoadBearing = true,
                    Floor = section.Floor,
                    MaterialType = material,
                    Color = color
                });
            }

            _logger.LogInformation(
                "Generated {Count} exterior wall segments from {Sections} sections",
                walls.Count, layout.Sections.Count);

            return walls;
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
