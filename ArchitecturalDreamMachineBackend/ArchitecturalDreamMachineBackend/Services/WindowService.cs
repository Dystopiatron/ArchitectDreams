using ArchitecturalDreamMachineBackend.Constants;
using ArchitecturalDreamMachineBackend.Geometry;
using ArchitecturalDreamMachineBackend.Models;

namespace ArchitecturalDreamMachineBackend.Services;

/// <summary>
/// Service for generating window geometries based on room layouts
/// Places windows on exterior walls respecting architectural proportions
/// </summary>
public class WindowService : IWindowService
{
    private readonly IGeometryService _geometryService;
    private readonly ILogger<WindowService> _logger;

    // Window sizing constants
    private const double StandardWindowWidth = 3.0;   // 3 feet wide
    private const double StandardWindowHeight = 4.0;  // 4 feet tall
    private const double WindowSillHeight = 3.0;      // 3 feet from floor to sill
    private const double MinWindowSpacing = 2.0;      // Minimum 2 feet between windows
    private const double WallEdgeMargin = 1.5;        // Margin from wall corners

    public WindowService(IGeometryService geometryService, ILogger<WindowService> logger)
    {
        _geometryService = geometryService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public List<GeometryData> GenerateWindows(
        List<Room> rooms,
        double windowToWallRatio,
        double ceilingHeight,
        double footprintWidth,
        double footprintDepth)
    {
        var windows = new List<GeometryData>();
        
        _logger.LogInformation(
            "Generating windows for {RoomCount} rooms with {Ratio:P0} window-to-wall ratio",
            rooms.Count, windowToWallRatio);

        // Group rooms by floor for proper Y positioning
        var roomsByFloor = rooms.GroupBy(r => r.Floor).OrderBy(g => g.Key);

        foreach (var floorGroup in roomsByFloor)
        {
            int floor = floorGroup.Key;
            double floorBaseY = (floor - 1) * ceilingHeight;
            double windowCenterY = floorBaseY + WindowSillHeight + (StandardWindowHeight / 2);

            foreach (var room in floorGroup)
            {
                var roomWindows = GenerateRoomWindows(
                    room,
                    windowToWallRatio,
                    windowCenterY,
                    footprintWidth,
                    footprintDepth);
                
                windows.AddRange(roomWindows);
            }
        }

        _logger.LogInformation("Generated {WindowCount} total windows", windows.Count);
        return windows;
    }

    /// <inheritdoc/>
    public List<WindowElement> GenerateWindowElements(
        List<Room> rooms,
        double windowToWallRatio,
        double ceilingHeight,
        double footprintWidth,
        double footprintDepth)
    {
        var windowElements = new List<WindowElement>();
        int windowIndex = 1;
        
        _logger.LogInformation(
            "Generating window elements for {RoomCount} rooms with wall relationships",
            rooms.Count);

        // Group rooms by floor for proper Y positioning
        var roomsByFloor = rooms.GroupBy(r => r.Floor).OrderBy(g => g.Key);

        foreach (var floorGroup in roomsByFloor)
        {
            int floor = floorGroup.Key;
            double floorBaseY = (floor - 1) * ceilingHeight;
            double windowCenterY = floorBaseY + WindowSillHeight + (StandardWindowHeight / 2);

            foreach (var room in floorGroup)
            {
                var roomWindowElements = GenerateRoomWindowElements(
                    room,
                    windowToWallRatio,
                    windowCenterY,
                    footprintWidth,
                    footprintDepth,
                    ref windowIndex);
                
                windowElements.AddRange(roomWindowElements);
            }
        }

        _logger.LogInformation("Generated {WindowCount} window elements with wall relationships", 
            windowElements.Count);
        return windowElements;
    }

    /// <inheritdoc/>
    public List<WindowElement> GenerateLinkedWindowElements(
        List<Room> rooms,
        double windowToWallRatio,
        double ceilingHeight,
        double footprintWidth,
        double footprintDepth,
        List<WallSegment> exteriorWalls)
    {
        // Generate window elements using the existing logic
        var elements = GenerateWindowElements(
            rooms, windowToWallRatio, ceilingHeight, footprintWidth, footprintDepth);

        // Link each window to the nearest matching exterior wall segment
        foreach (var window in elements)
        {
            var matched = FindMatchingExteriorWall(window, exteriorWalls);
            if (matched != null)
            {
                window.WallSegmentId = matched.Id;
                matched.Openings.Add(new Opening
                {
                    Type = OpeningType.Window,
                    ElementId = window.Id,
                    PositionAlongWall = window.OffsetAlongWall,
                    Width = window.Width,
                    Height = window.Height,
                    SillHeight = window.SillHeight
                });
            }
        }

        _logger.LogInformation(
            "Linked {Linked}/{Total} window elements to exterior wall segments",
            elements.Count(w => !string.IsNullOrEmpty(w.WallSegmentId)),
            elements.Count);

        return elements;
    }

    /// <summary>
    /// Find the exterior wall segment that best matches a window's position and direction
    /// </summary>
    private static WallSegment? FindMatchingExteriorWall(WindowElement window, List<WallSegment> walls)
    {
        WallSegment? best = null;
        double bestDist = double.MaxValue;

        foreach (var wall in walls)
        {
            if (wall.Floor != window.Floor) continue;

            // Determine wall orientation: is it along X or Z?
            bool wallAlongX = Math.Abs(wall.StartZ - wall.EndZ) < 0.01;
            bool wallAlongZ = Math.Abs(wall.StartX - wall.EndX) < 0.01;

            // Match wall orientation to window direction
            bool directionMatch = window.WallDirection switch
            {
                Geometry.WallDirection.Front or Geometry.WallDirection.Back => wallAlongX,
                Geometry.WallDirection.Left or Geometry.WallDirection.Right => wallAlongZ,
                _ => false
            };

            if (!directionMatch) continue;

            // Check if window position falls within the wall's span
            double wallMidX = (wall.StartX + wall.EndX) / 2.0;
            double wallMidZ = (wall.StartZ + wall.EndZ) / 2.0;

            double dist = Math.Abs(window.X - wallMidX) + Math.Abs(window.Z - wallMidZ);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = wall;
            }
        }

        return best;
    }

    /// <summary>
    /// Generate window elements for a single room on exterior walls
    /// </summary>
    private List<WindowElement> GenerateRoomWindowElements(
        Room room,
        double windowToWallRatio,
        double windowCenterY,
        double footprintWidth,
        double footprintDepth,
        ref int windowIndex)
    {
        var elements = new List<WindowElement>();
        
        // Calculate number of windows based on room's WindowCount property
        int windowCount = room.WindowCount > 0 
            ? room.WindowCount 
            : CalculateWindowCount(room.Width, room.Depth, windowToWallRatio);

        if (windowCount == 0) return elements;

        // Determine which walls are exterior walls
        var exteriorWalls = DetermineExteriorWalls(room, footprintWidth, footprintDepth);

        if (!exteriorWalls.Any())
        {
            _logger.LogDebug("Room {RoomName} has no exterior walls, skipping windows", room.Name);
            return elements;
        }

        // Distribute windows across exterior walls
        int windowsPerWall = Math.Max(1, windowCount / exteriorWalls.Count);
        int remainingWindows = windowCount;

        foreach (var wall in exteriorWalls)
        {
            int wallWindows = Math.Min(windowsPerWall, remainingWindows);
            if (wallWindows == 0) break;

            var wallWindowElements = GenerateWallWindowElements(
                wall, 
                wallWindows, 
                windowCenterY,
                room,
                ref windowIndex);

            elements.AddRange(wallWindowElements);
            remainingWindows -= wallWindowElements.Count;
        }

        return elements;
    }

    /// <summary>
    /// Generate window elements along a single wall with full relationship data
    /// </summary>
    private List<WindowElement> GenerateWallWindowElements(
        ExteriorWall wall,
        int windowCount,
        double windowCenterY,
        Room room,
        ref int windowIndex)
    {
        var elements = new List<WindowElement>();
        
        // Check if wall is long enough for windows
        double availableLength = wall.Length - (2 * WallEdgeMargin);
        if (availableLength < StandardWindowWidth) return elements;

        // Calculate maximum windows that can fit
        double requiredLengthPerWindow = StandardWindowWidth + MinWindowSpacing;
        int maxWindows = (int)((availableLength + MinWindowSpacing) / requiredLengthPerWindow);
        windowCount = Math.Min(windowCount, maxWindows);

        if (windowCount == 0) return elements;

        // Calculate spacing between windows
        double totalWindowWidth = windowCount * StandardWindowWidth;
        double totalSpacing = availableLength - totalWindowWidth;
        double spacing = totalSpacing / (windowCount + 1);

        // Generate each window element
        for (int i = 0; i < windowCount; i++)
        {
            double offset = WallEdgeMargin + spacing + (StandardWindowWidth / 2) + 
                           (i * (StandardWindowWidth + spacing));
            
            double windowX, windowZ;
            double rotation = 0;

            switch (wall.Direction)
            {
                case WallDirection.Front:
                case WallDirection.Back:
                    windowX = wall.CenterX - (wall.Length / 2) + offset;
                    windowZ = wall.CenterZ + (wall.Direction == WallDirection.Front ? 0.1 : -0.1);
                    break;
                    
                case WallDirection.Left:
                case WallDirection.Right:
                    windowZ = wall.CenterZ - (wall.Length / 2) + offset;
                    windowX = wall.CenterX + (wall.Direction == WallDirection.Right ? 0.1 : -0.1);
                    rotation = Math.PI / 2;
                    break;
                    
                default:
                    continue;
            }

            var element = new WindowElement
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"Window {windowIndex++}",
                RoomName = room.Name,
                Floor = room.Floor,
                Width = StandardWindowWidth,
                Height = StandardWindowHeight,
                SillHeight = WindowSillHeight,
                X = windowX,
                Y = windowCenterY,
                Z = windowZ,
                RotationY = rotation,
                WallDirection = MapToPublicWallDirection(wall.Direction),
                OffsetAlongWall = offset,
                WallThickness = 0.5, // Standard wall thickness
                MaterialType = "glass",
                Color = GetWindowColor(room.Name)
            };

            elements.Add(element);
        }

        return elements;
    }

    /// <summary>
    /// Map internal WallDirection to public enum
    /// </summary>
    private Geometry.WallDirection MapToPublicWallDirection(WallDirection direction)
    {
        return direction switch
        {
            WallDirection.Front => Geometry.WallDirection.Front,
            WallDirection.Back => Geometry.WallDirection.Back,
            WallDirection.Left => Geometry.WallDirection.Left,
            WallDirection.Right => Geometry.WallDirection.Right,
            _ => Geometry.WallDirection.Front
        };
    }

    /// <summary>
    /// Generate windows for a single room on exterior walls
    /// </summary>
    private List<GeometryData> GenerateRoomWindows(
        Room room,
        double windowToWallRatio,
        double windowCenterY,
        double footprintWidth,
        double footprintDepth)
    {
        var windows = new List<GeometryData>();
        
        // Calculate number of windows based on room's WindowCount property
        // or calculate from wall area and ratio
        int windowCount = room.WindowCount > 0 
            ? room.WindowCount 
            : CalculateWindowCount(room.Width, room.Depth, windowToWallRatio);

        if (windowCount == 0) return windows;

        // Determine which walls are exterior walls
        var exteriorWalls = DetermineExteriorWalls(room, footprintWidth, footprintDepth);

        if (!exteriorWalls.Any())
        {
            _logger.LogDebug("Room {RoomName} has no exterior walls, skipping windows", room.Name);
            return windows;
        }

        // Distribute windows across exterior walls
        int windowsPerWall = Math.Max(1, windowCount / exteriorWalls.Count);
        int remainingWindows = windowCount;

        foreach (var wall in exteriorWalls)
        {
            int wallWindows = Math.Min(windowsPerWall, remainingWindows);
            if (wallWindows == 0) break;

            var wallWindowGeometries = GenerateWallWindows(
                wall, 
                wallWindows, 
                windowCenterY,
                room);

            windows.AddRange(wallWindowGeometries);
            remainingWindows -= wallWindowGeometries.Count;
        }

        return windows;
    }

    /// <summary>
    /// Determine which walls of a room are exterior walls
    /// </summary>
    private List<ExteriorWall> DetermineExteriorWalls(
        Room room,
        double footprintWidth,
        double footprintDepth)
    {
        var walls = new List<ExteriorWall>();

        // Rooms are defined in 0-based space (X/Z = min-corner, 0..footprint).
        // Building geometry is centered at origin: -footprint/2..+footprint/2.
        // Translate room bounds to building space.
        double roomMinX = room.X - footprintWidth / 2;
        double roomMaxX = room.X + room.Width - footprintWidth / 2;
        double roomMinZ = room.Z - footprintDepth / 2;
        double roomMaxZ = room.Z + room.Depth - footprintDepth / 2;

        // Room center in building space (used as CenterX/CenterZ for wall quads)
        double roomCenterX = (roomMinX + roomMaxX) / 2;
        double roomCenterZ = (roomMinZ + roomMaxZ) / 2;

        // Building bounds (centered at origin)
        double buildingMinX = -footprintWidth / 2;
        double buildingMaxX = footprintWidth / 2;
        double buildingMinZ = -footprintDepth / 2;
        double buildingMaxZ = footprintDepth / 2;

        const double tolerance = 0.5; // Tolerance for edge detection

        // Check if room edges align with building exterior
        // Front wall (positive Z)
        if (Math.Abs(roomMaxZ - buildingMaxZ) < tolerance)
        {
            walls.Add(new ExteriorWall
            {
                Direction = WallDirection.Front,
                Length = room.Width,
                CenterX = roomCenterX,
                CenterZ = roomMaxZ,
                FacingZ = 0.1
            });
        }

        // Back wall (negative Z)
        if (Math.Abs(roomMinZ - buildingMinZ) < tolerance)
        {
            walls.Add(new ExteriorWall
            {
                Direction = WallDirection.Back,
                Length = room.Width,
                CenterX = roomCenterX,
                CenterZ = roomMinZ,
                FacingZ = -0.1
            });
        }

        // Right wall (positive X)
        if (Math.Abs(roomMaxX - buildingMaxX) < tolerance)
        {
            walls.Add(new ExteriorWall
            {
                Direction = WallDirection.Right,
                Length = room.Depth,
                CenterX = roomMaxX,
                CenterZ = roomCenterZ,
                FacingX = 0.1
            });
        }

        // Left wall (negative X)
        if (Math.Abs(roomMinX - buildingMinX) < tolerance)
        {
            walls.Add(new ExteriorWall
            {
                Direction = WallDirection.Left,
                Length = room.Depth,
                CenterX = roomMinX,
                CenterZ = roomCenterZ,
                FacingX = -0.1
            });
        }

        return walls;
    }

    /// <summary>
    /// Generate windows along a single wall
    /// </summary>
    private List<GeometryData> GenerateWallWindows(
        ExteriorWall wall,
        int windowCount,
        double windowCenterY,
        Room room)
    {
        var windows = new List<GeometryData>();
        
        // Check if wall is long enough for windows
        double availableLength = wall.Length - (2 * WallEdgeMargin);
        if (availableLength < StandardWindowWidth) return windows;

        // Calculate maximum windows that can fit
        double requiredLengthPerWindow = StandardWindowWidth + MinWindowSpacing;
        int maxWindows = (int)((availableLength + MinWindowSpacing) / requiredLengthPerWindow);
        windowCount = Math.Min(windowCount, maxWindows);

        if (windowCount == 0) return windows;

        // Calculate spacing between windows
        double totalWindowWidth = windowCount * StandardWindowWidth;
        double totalSpacing = availableLength - totalWindowWidth;
        double spacing = totalSpacing / (windowCount + 1);

        // Generate each window
        for (int i = 0; i < windowCount; i++)
        {
            double offset = WallEdgeMargin + spacing + (StandardWindowWidth / 2) + 
                           (i * (StandardWindowWidth + spacing));
            
            double windowX, windowZ;
            double rotation = 0;

            switch (wall.Direction)
            {
                case WallDirection.Front:
                case WallDirection.Back:
                    // Windows along X axis
                    windowX = wall.CenterX - (wall.Length / 2) + offset;
                    windowZ = wall.CenterZ + (wall.Direction == WallDirection.Front ? 0.1 : -0.1);
                    break;
                    
                case WallDirection.Left:
                case WallDirection.Right:
                    // Windows along Z axis, rotated 90 degrees
                    windowZ = wall.CenterZ - (wall.Length / 2) + offset;
                    windowX = wall.CenterX + (wall.Direction == WallDirection.Right ? 0.1 : -0.1);
                    rotation = Math.PI / 2; // 90 degrees
                    break;
                    
                default:
                    continue;
            }

            var windowGeom = _geometryService.CreateQuad(
                StandardWindowWidth,
                StandardWindowHeight,
                windowX,
                windowCenterY,
                windowZ,
                "glass",
                GetWindowColor(room.Name));

            // Add rotation data for side walls
            if (rotation != 0)
            {
                windowGeom.Rotation = new Rotation { Y = rotation };
            }

            windows.Add(windowGeom);
        }

        return windows;
    }

    /// <summary>
    /// Calculate window count based on wall area and ratio
    /// </summary>
    private int CalculateWindowCount(double width, double depth, double windowToWallRatio)
    {
        // Perimeter walls area (simplified: 2 walls with width, 2 with depth)
        double wallPerimeter = 2 * width + 2 * depth;
        double wallHeight = ArchitecturalConstants.DefaultCeilingHeight;
        double totalWallArea = wallPerimeter * wallHeight;
        
        // Calculate window area needed
        double windowArea = totalWallArea * windowToWallRatio;
        
        // Calculate number of standard windows
        int windowCount = (int)(windowArea / ArchitecturalConstants.DefaultWindowArea);
        
        // Clamp to reasonable range
        return Math.Clamp(windowCount, 
            ArchitecturalConstants.MinWindowsPerRoom, 
            ArchitecturalConstants.MaxWindowsPerRoom);
    }

    /// <summary>
    /// Get window color based on room type
    /// </summary>
    private string GetWindowColor(string roomName)
    {
        // Slightly different tints for visual variety
        return roomName.ToLower() switch
        {
            string s when s.Contains("bathroom") => "#87ceeb", // Light blue (frosted)
            string s when s.Contains("bedroom") => "#a3d5e8",  // Slightly darker blue
            _ => "#b0e0e6" // Default light blue (powder blue)
        };
    }

    /// <summary>
    /// Represents an exterior wall where windows can be placed
    /// </summary>
    private class ExteriorWall
    {
        public WallDirection Direction { get; set; }
        public double Length { get; set; }
        public double CenterX { get; set; }
        public double CenterZ { get; set; }
        public double FacingX { get; set; }
        public double FacingZ { get; set; }
    }

    private enum WallDirection
    {
        Front,  // +Z
        Back,   // -Z
        Left,   // -X
        Right   // +X
    }
}
