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
        private readonly IWallFaceService _wallFaceService;
        private readonly ILogger<DesignOrchestrationService> _logger;

        public DesignOrchestrationService(
            IGeometryService geometryService,
            ILayoutService layoutService,
            IRoofService roofService,
            IWindowService windowService,
            IInteriorWallService interiorWallService,
            IWallFaceService wallFaceService,
            ILogger<DesignOrchestrationService> logger)
        {
            _geometryService = geometryService;
            _layoutService = layoutService;
            _roofService = roofService;
            _windowService = windowService;
            _interiorWallService = interiorWallService;
            _wallFaceService = wallFaceService;
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
            // For split-level and angled layouts, rooms are defined in full-footprint space
            // but sections are smaller/offset. Generate windows per-section so that exterior
            // wall detection uses the correct section dimensions.
            List<GeometryData> windows;
            List<WindowElement> windowElements;

            if ((parameters.BuildingShape == "split-level" || parameters.BuildingShape == "angled") && layout.Sections.Count > 1)
            {
                windows = new List<GeometryData>();
                windowElements = new List<WindowElement>();

                foreach (var section in layout.Sections)
                {
                    var sectionRooms = ClampRoomsToSection(
                        parameters.Rooms, section,
                        parameters.FootprintWidth, parameters.FootprintDepth,
                        parameters.CeilingHeight);

                    // For sections that extend beyond the footprint (like angled wing),
                    // clamped rooms won't reach the actual exterior edges. Add a synthetic
                    // room that fills the section to ensure windows on all exterior faces.
                    bool sectionExtendsBeyondFootprint =
                        section.X - section.Width / 2 < -parameters.FootprintWidth / 2 - 0.1 ||
                        section.X + section.Width / 2 > parameters.FootprintWidth / 2 + 0.1 ||
                        section.Z - section.Depth / 2 < -parameters.FootprintDepth / 2 - 0.1 ||
                        section.Z + section.Depth / 2 > parameters.FootprintDepth / 2 + 0.1;

                    if (sectionExtendsBeyondFootprint && section.AddWindows)
                    {
                        // Add a section-filling room to ensure windows on all exterior edges
                        int floorsInSection = (int)(section.Height / parameters.CeilingHeight);
                        for (int f = 1; f <= floorsInSection; f++)
                        {
                            sectionRooms.Add(new Room
                            {
                                Name = $"Wing Room {f}",
                                Floor = f,
                                X = 0,
                                Z = 0,
                                Width = section.Width,
                                Depth = section.Depth,
                                WindowCount = 4,
                                HasDoor = false
                            });
                        }
                    }

                    if (!sectionRooms.Any()) continue;

                    var secWindows = _windowService.GenerateWindows(
                        sectionRooms,
                        parameters.WindowToWallRatio,
                        parameters.CeilingHeight,
                        section.Width, section.Depth,
                        parameters.BuildingShape,
                        parameters.WindowStyle);

                    var secElements = _windowService.GenerateWindowElements(
                        sectionRooms,
                        parameters.WindowToWallRatio,
                        parameters.CeilingHeight,
                        section.Width, section.Depth,
                        parameters.BuildingShape,
                        parameters.WindowStyle);

                    // Offset from section-local to building-global coordinates
                    // Windows are generated in section-centered coords, add section center to get building coords
                    foreach (var w in secWindows)
                    {
                        if (w.Position != null)
                        {
                            w.Position.X += section.X;
                            w.Position.Z += section.Z;
                        }
                    }
                    foreach (var we in secElements)
                    {
                        we.X += section.X;
                        we.Z += section.Z;
                    }

                    windows.AddRange(secWindows);
                    windowElements.AddRange(secElements);
                }
            }
            else
            {
                windows = _windowService.GenerateWindows(
                    parameters.Rooms,
                    parameters.WindowToWallRatio,
                    parameters.CeilingHeight,
                    parameters.FootprintWidth,
                    parameters.FootprintDepth,
                    parameters.BuildingShape,
                    parameters.WindowStyle);

                windowElements = _windowService.GenerateWindowElements(
                    parameters.Rooms,
                    parameters.WindowToWallRatio,
                    parameters.CeilingHeight,
                    parameters.FootprintWidth,
                    parameters.FootprintDepth,
                    parameters.BuildingShape,
                    parameters.WindowStyle);
            }
            
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
            // windows and interior walls whose XYZ position falls outside all building sections.
            // This prevents floating geometry in the "void" areas of compound footprints.
            // Important: We check XYZ together per section, not XZ and Y separately, because
            // a point might be in one section's XZ footprint but another section's Y range.
            if (layout.Sections.Count > 1 || parameters.BuildingShape is "l-shape" or "angled" or "split-level")
            {
                windows        = windows.Where(w        => IsWithinAnySectionXYZ(w.Position?.X ?? 0, w.Position?.Y ?? 0, w.Position?.Z ?? 0, layout.Sections)).ToList();
                windowElements = windowElements.Where(w => IsWithinAnySectionXYZ(w.X, w.Y, w.Z, layout.Sections)).ToList();
                interiorWalls  = interiorWalls.Where(w  => IsWithinAnySectionXYZ(w.Position?.X ?? 0, w.Position?.Y ?? 0, w.Position?.Z ?? 0, layout.Sections)).ToList();
            }

            // Step 5d: Generate perforated wall face panels for Three.js ShapeGeometry rendering
            var wallFaceResult = _wallFaceService.GenerateWallFaces(
                layout.Sections,
                windowElements,
                doorElements,
                parameters.ExteriorMaterial,
                parameters.Material?.Color ?? "white");
            var wallFaces = wallFaceResult.Faces;

            // Step 5e: Filter window geometry to only include windows placed on visible faces.
            // Windows inside overlap zones (hidden behind other sections) are removed.
            var placedIds = wallFaceResult.PlacedWindowIds;
            windowElements = windowElements.Where(we => placedIds.Contains(we.Id)).ToList();
            windows = windows.Where(w =>
                windowElements.Any(we =>
                    Math.Abs((w.Position?.X ?? 0) - we.X) < 0.15 &&
                    Math.Abs((w.Position?.Y ?? 0) - we.Y) < 0.15 &&
                    Math.Abs((w.Position?.Z ?? 0) - we.Z) < 0.15)
            ).ToList();

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
                WallFaces = wallFaces,
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
            double tolerance = 0.5)
        {
            return sections.Any(s =>
                x >= s.X - s.Width  / 2 - tolerance &&
                x <= s.X + s.Width  / 2 + tolerance &&
                z >= s.Z - s.Depth / 2 - tolerance &&
                z <= s.Z + s.Depth / 2 + tolerance);
        }

        /// <summary>
        /// Returns true if the Y coordinate falls within the vertical bounds
        /// of at least one building section.
        /// </summary>
        private static bool IsWithinAnySectionY(
            double y,
            List<LayoutSection> sections,
            double tolerance = 0.5)
        {
            return sections.Any(s =>
                y >= s.Y - s.Height / 2 - tolerance &&
                y <= s.Y + s.Height / 2 + tolerance);
        }

        /// <summary>
        /// Returns true if the point (x, y, z) falls within the 3D bounds
        /// of at least one building section. All three coordinates must be
        /// within the SAME section's bounds (not a combination of different sections).
        /// This is important for layouts like split-level where sections have different heights.
        /// </summary>
        private static bool IsWithinAnySectionXYZ(
            double x, double y, double z,
            List<LayoutSection> sections,
            double tolerance = 0.5)
        {
            return sections.Any(s =>
                x >= s.X - s.Width  / 2 - tolerance &&
                x <= s.X + s.Width  / 2 + tolerance &&
                y >= s.Y - s.Height / 2 - tolerance &&
                y <= s.Y + s.Height / 2 + tolerance &&
                z >= s.Z - s.Depth / 2 - tolerance &&
                z <= s.Z + s.Depth / 2 + tolerance);
        }

        /// <summary>
        /// Clamp rooms to a section's XZ bounds and convert to section-local 0-based coordinates.
        /// Rooms are defined in full-footprint 0-based space (X/Z ∈ [0, footprint]).
        /// This method clips each room to the section's world-space bounds and re-expresses
        /// coordinates so WindowService sees them as flush with the section edges.
        /// 
        /// For multi-story sections (like angled tower), includes rooms from ALL floors
        /// that fall within the section's vertical Y range, not just the section.Floor.
        /// </summary>
        private static List<Room> ClampRoomsToSection(
            List<Room> rooms, LayoutSection section,
            double footprintWidth, double footprintDepth,
            double ceilingHeight)
        {
            var result = new List<Room>();

            // Section bounds in building space (centered at origin)
            double secMinX = section.X - section.Width / 2;
            double secMaxX = section.X + section.Width / 2;
            double secMinZ = section.Z - section.Depth / 2;
            double secMaxZ = section.Z + section.Depth / 2;
            
            // Section Y bounds
            double secBaseY = section.Y - section.Height / 2;
            double secTopY = section.Y + section.Height / 2;

            foreach (var room in rooms)
            {
                // Check if room's floor falls within section's Y range
                double roomFloorBaseY = (room.Floor - 1) * ceilingHeight;
                double roomFloorTopY = room.Floor * ceilingHeight;
                
                // Room belongs to this section if there's vertical overlap
                if (roomFloorTopY <= secBaseY || roomFloorBaseY >= secTopY)
                    continue;
                // Room bounds in building space (rooms are 0-based, translated by -footprint/2)
                double roomBuildMinX = room.X - footprintWidth / 2;
                double roomBuildMaxX = roomBuildMinX + room.Width;
                double roomBuildMinZ = room.Z - footprintDepth / 2;
                double roomBuildMaxZ = roomBuildMinZ + room.Depth;

                // Clamp to section bounds
                double clampMinX = Math.Max(roomBuildMinX, secMinX);
                double clampMaxX = Math.Min(roomBuildMaxX, secMaxX);
                double clampMinZ = Math.Max(roomBuildMinZ, secMinZ);
                double clampMaxZ = Math.Min(roomBuildMaxZ, secMaxZ);

                double w = clampMaxX - clampMinX;
                double d = clampMaxZ - clampMinZ;
                if (w < 1 || d < 1) continue; // Skip rooms too small after clamping

                // Convert to section-local 0-based space
                result.Add(new Room
                {
                    Name = room.Name,
                    Floor = room.Floor,
                    X = clampMinX - secMinX,
                    Z = clampMinZ - secMinZ,
                    Width = w,
                    Depth = d,
                    WindowCount = room.WindowCount,
                    HasDoor = room.HasDoor
                });
            }

            return result;
        }
    }
}
