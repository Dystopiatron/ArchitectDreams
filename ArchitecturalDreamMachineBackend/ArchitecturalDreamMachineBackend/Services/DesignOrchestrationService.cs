using ArchitecturalDreamMachineBackend.Models;
using ArchitecturalDreamMachineBackend.Data;
using ArchitecturalDreamMachineBackend.Geometry;

namespace ArchitecturalDreamMachineBackend.Services
{
    /// <summary>
    /// Orchestrates all geometry generation services to produce complete building geometry
    /// This is the main entry point that coordinates layout, roof, and geometry services
    /// </summary>
    public class DesignOrchestrationService
    {
        private readonly GeometryService _geometryService;
        private readonly LayoutService _layoutService;
        private readonly RoofService _roofService;
        private readonly ILogger<DesignOrchestrationService> _logger;
        
        public DesignOrchestrationService(
            GeometryService geometryService,
            LayoutService layoutService,
            RoofService roofService,
            ILogger<DesignOrchestrationService> logger)
        {
            _geometryService = geometryService;
            _layoutService = layoutService;
            _roofService = roofService;
            _logger = logger;
        }
        
        /// <summary>
        /// Generate complete building geometry ready for frontend rendering
        /// </summary>
        /// <param name="parameters">House parameters from design generation</param>
        /// <returns>Complete building geometry with all sections, roofs, etc.</returns>
        public BuildingGeometry GenerateCompleteGeometry(HouseParameters parameters)
        {
            _logger.LogInformation(
                "Generating complete geometry: style={Style}, shape={Shape}, {Width}x{Depth}x{Stories}",
                parameters.ExteriorMaterial, parameters.BuildingShape, 
                parameters.FootprintWidth, parameters.FootprintDepth, parameters.Stories);
            
            var startTime = DateTime.UtcNow;
            
            // Step 1: Determine layout (sections and roof placements)
            var layout = _layoutService.DetermineLayout(
                parameters.ExteriorMaterial ?? "modern",
                parameters.BuildingShape ?? "cube",
                parameters.FootprintWidth,
                parameters.FootprintDepth,
                parameters.CeilingHeight,
                parameters.Stories);
            
            // Step 2: Generate building section geometries
            var sectionGeometries = layout.Sections.Select(section =>
                _geometryService.CreateBox(
                    section.Width,
                    section.Height,
                    section.Depth,
                    section.X,
                    section.Y,
                    section.Z,
                    parameters.ExteriorMaterial ?? "stucco",
                    parameters.Material?.Color ?? "white")
            ).ToList();
            
            // Step 3: Generate roof geometries
            var roofGeometries = _roofService.CalculateRoofs(
                layout.RoofSections,
                parameters.RoofType ?? "flat",
                parameters.RoofPitch,
                parameters.HasEaves ? (parameters.EavesOverhang > 0 ? parameters.EavesOverhang : 1.5) : 0,
                parameters.HasParapet);
            
            // Step 4: Calculate total height and max dimension
            var maxRoofHeight = roofGeometries.Any() ? roofGeometries.Max(r => r.Height) : 0;
            var totalHeight = layout.TotalHeight + maxRoofHeight;
            var maxDimension = Math.Max(layout.TotalWidth, layout.TotalDepth);
            
            // Step 5: Assemble complete geometry
            var buildingGeometry = new BuildingGeometry
            {
                Sections = sectionGeometries,
                Roofs = roofGeometries,
                TotalHeight = totalHeight,
                MaxDimension = maxDimension,
                Windows = new List<GeometryData>(), // TODO: Window generation
                InteriorWalls = new List<GeometryData>() // TODO: Interior wall generation
            };
            
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation(
                "Geometry generated in {Elapsed}ms: {Sections} sections, {Roofs} roofs, height={Height:F1}",
                elapsed, sectionGeometries.Count, roofGeometries.Count, totalHeight);
            
            return buildingGeometry;
        }
    }
}
