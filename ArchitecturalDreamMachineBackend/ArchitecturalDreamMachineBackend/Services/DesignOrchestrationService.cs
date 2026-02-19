using ArchitecturalDreamMachineBackend.Constants;
using ArchitecturalDreamMachineBackend.Models;
using ArchitecturalDreamMachineBackend.Data;
using ArchitecturalDreamMachineBackend.Geometry;

namespace ArchitecturalDreamMachineBackend.Services
{
    /// <summary>
    /// Orchestrates all geometry generation services to produce complete building geometry
    /// This is the main entry point that coordinates layout, roof, and geometry services
    /// </summary>
    public class DesignOrchestrationService : IDesignOrchestrationService
    {
        private readonly IGeometryService _geometryService;
        private readonly ILayoutService _layoutService;
        private readonly IRoofService _roofService;
        private readonly IWindowService _windowService;
        private readonly IInteriorWallService _interiorWallService;
        private readonly ILogger<DesignOrchestrationService> _logger;
        
        public DesignOrchestrationService(
            IGeometryService geometryService,
            ILayoutService layoutService,
            IRoofService roofService,
            IWindowService windowService,
            IInteriorWallService interiorWallService,
            ILogger<DesignOrchestrationService> logger)
        {
            _geometryService = geometryService;
            _layoutService = layoutService;
            _roofService = roofService;
            _windowService = windowService;
            _interiorWallService = interiorWallService;
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
                parameters.ExteriorMaterial,
                parameters.BuildingShape,
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
                    parameters.ExteriorMaterial,
                    parameters.Material?.Color ?? "white")
            ).ToList();
            
            // Step 3: Generate roof geometries
            var roofGeometries = _roofService.CalculateRoofs(
                layout.RoofSections,
                parameters.RoofType,
                parameters.RoofPitch,
                parameters.HasEaves ? (parameters.EavesOverhang > 0 ? parameters.EavesOverhang : ArchitecturalConstants.DefaultEavesOverhang) : 0,
                parameters.HasParapet);
            
            // Step 4: Generate windows
            var windows = _windowService.GenerateWindows(
                parameters.Rooms,
                parameters.WindowToWallRatio,
                parameters.CeilingHeight,
                parameters.FootprintWidth,
                parameters.FootprintDepth,
                parameters.BuildingShape);

            // Step 4b: Generate window elements with wall relationships for BIM export
            var windowElements = _windowService.GenerateWindowElements(
                parameters.Rooms,
                parameters.WindowToWallRatio,
                parameters.CeilingHeight,
                parameters.FootprintWidth,
                parameters.FootprintDepth,
                parameters.BuildingShape);
            
            // Step 5: Generate interior walls
            var interiorWalls = _interiorWallService.GenerateInteriorWalls(
                parameters.Rooms,
                parameters.CeilingHeight,
                parameters.FootprintWidth,
                parameters.FootprintDepth,
                parameters.BuildingShape);

            // Step 5b: Generate door elements with wall relationships for BIM export
            var doorElements = _interiorWallService.GenerateDoorElements(
                parameters.Rooms,
                parameters.CeilingHeight,
                parameters.FootprintWidth,
                parameters.FootprintDepth,
                parameters.BuildingShape);
            
            // Step 5c: For non-rectangular layouts (L-shape, angled, split-level) filter out
            // windows and interior walls whose XZ position falls outside all building sections.
            // This prevents floating geometry in the "void" areas of compound footprints.
            if (layout.Sections.Count > 1 || parameters.BuildingShape is "l-shape" or "angled" or "split-level")
            {
                windows        = windows.Where(w        => IsWithinAnySectionXZ(w.Position?.X ?? 0, w.Position?.Z ?? 0, layout.Sections)).ToList();
                interiorWalls  = interiorWalls.Where(w  => IsWithinAnySectionXZ(w.Position?.X ?? 0, w.Position?.Z ?? 0, layout.Sections)).ToList();
            }

            // Step 6: Calculate total height and max dimension
            var maxRoofHeight = roofGeometries.Any() ? roofGeometries.Max(r => r.Height) : 0;
            var totalHeight = layout.TotalHeight + maxRoofHeight;
            var maxDimension = Math.Max(layout.TotalWidth, layout.TotalDepth);
            
            // Step 7: Build semantic model with typed wall segments and linked openings
            var exteriorWallSegments = _layoutService.GenerateExteriorWalls(
                layout,
                parameters.CeilingHeight,
                parameters.Stories,
                parameters.ExteriorMaterial,
                parameters.Material?.Color ?? "white");

            var interiorWallSegments = _interiorWallService.GenerateInteriorWallSegments(
                parameters.Rooms,
                parameters.CeilingHeight,
                parameters.FootprintWidth,
                parameters.FootprintDepth,
                parameters.BuildingShape);

            var linkedWindows = _windowService.GenerateLinkedWindowElements(
                parameters.Rooms,
                parameters.WindowToWallRatio,
                parameters.CeilingHeight,
                parameters.FootprintWidth,
                parameters.FootprintDepth,
                exteriorWallSegments,
                parameters.BuildingShape);

            var linkedDoors = _interiorWallService.GenerateLinkedDoorElements(
                parameters.Rooms,
                parameters.CeilingHeight,
                parameters.FootprintWidth,
                parameters.FootprintDepth,
                interiorWallSegments,
                parameters.BuildingShape);

            var semanticModel = new BuildingModel
            {
                Floors = BuildFloors(parameters),
                ExteriorWalls = exteriorWallSegments,
                InteriorWalls = interiorWallSegments,
                Windows = linkedWindows,
                Doors = linkedDoors,
                Slabs = BuildSlabs(parameters),
                Roof = BuildRoofAssembly(parameters, layout),
                Stories = parameters.Stories,
                GrossFloorArea = parameters.FootprintWidth * parameters.FootprintDepth * parameters.Stories
            };

            // Step 8: Assemble complete geometry
            var buildingGeometry = new BuildingGeometry
            {
                Sections = sectionGeometries,
                Roofs = roofGeometries,
                Windows = windows,
                WindowElements = windowElements,
                InteriorWalls = interiorWalls,
                DoorElements = doorElements,
                TotalHeight = totalHeight,
                MaxDimension = maxDimension,
                SemanticModel = semanticModel
            };

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation(
                "Geometry generated in {Elapsed}ms: {Sections} sections, {Roofs} roofs, {Windows} windows, {Walls} interior walls, " +
                "{ExtWalls} exterior wall segments, {IntWalls} interior wall segments, height={Height:F1}",
                elapsed, sectionGeometries.Count, roofGeometries.Count, windows.Count, interiorWalls.Count,
                exteriorWallSegments.Count, interiorWallSegments.Count, totalHeight);

            return buildingGeometry;
        }

        private static List<Floor> BuildFloors(HouseParameters parameters)
        {
            var floors = new List<Floor>();
            for (int i = 1; i <= parameters.Stories; i++)
            {
                floors.Add(new Floor
                {
                    FloorNumber = i,
                    Elevation = (i - 1) * parameters.CeilingHeight,
                    RoomNames = parameters.Rooms
                        .Where(r => r.Floor == i)
                        .Select(r => r.Name)
                        .ToList()
                });
            }
            return floors;
        }

        private static List<Slab> BuildSlabs(HouseParameters parameters)
        {
            var slabs = new List<Slab>();
            for (int i = 1; i <= parameters.Stories; i++)
            {
                // Floor slab at base of each storey
                slabs.Add(new Slab
                {
                    SlabType = "Floor",
                    FloorNumber = i,
                    Width = parameters.FootprintWidth,
                    Depth = parameters.FootprintDepth
                });
                // Ceiling slab at top of each storey
                slabs.Add(new Slab
                {
                    SlabType = "Ceiling",
                    FloorNumber = i,
                    Width = parameters.FootprintWidth,
                    Depth = parameters.FootprintDepth
                });
            }
            return slabs;
        }

        private static RoofAssembly BuildRoofAssembly(HouseParameters parameters, LayoutData layout)
        {
            double wallsHeight = parameters.Stories * parameters.CeilingHeight;
            double peakHeight = Math.Max(0, layout.TotalHeight - wallsHeight);
            return new RoofAssembly
            {
                RoofType = parameters.RoofType,
                Pitch = parameters.RoofPitch,
                PeakHeight = peakHeight,
                HasEaves = parameters.HasEaves,
                EavesOverhang = parameters.EavesOverhang
            };
        }

        /// <summary>
        /// Returns true if the point (x, z) falls within the horizontal (XZ) bounds
        /// of at least one building section.  Tolerance accounts for windows placed
        /// just outside the wall surface (~0.1 ft offset) plus wall thickness.
        /// </summary>
        private static bool IsWithinAnySectionXZ(
            double x, double z,
            List<LayoutSection> sections,
            double tolerance = 1.5)
        {
            return sections.Any(s =>
                x >= s.X - s.Width  / 2 - tolerance &&
                x <= s.X + s.Width  / 2 + tolerance &&
                z >= s.Z - s.Depth / 2 - tolerance &&
                z <= s.Z + s.Depth / 2 + tolerance);
        }
    }
}
