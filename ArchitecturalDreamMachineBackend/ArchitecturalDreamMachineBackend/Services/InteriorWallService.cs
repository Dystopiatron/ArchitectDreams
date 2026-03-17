using ArchitecturalDreamMachineBackend.Constants;
using ArchitecturalDreamMachineBackend.Geometry;
using ArchitecturalDreamMachineBackend.Models;

namespace ArchitecturalDreamMachineBackend.Services;

/// <summary>
/// Interface for interior wall generation
/// </summary>
public interface IInteriorWallService
{
    /// <summary>
    /// Generate interior wall geometries between rooms
    /// </summary>
    /// <param name="rooms">Room layout data</param>
    /// <param name="ceilingHeight">Height of each floor</param>
    /// <param name="footprintWidth">Total building width</param>
    /// <param name="footprintDepth">Total building depth</param>
    /// <returns>List of interior wall geometries</returns>
    List<GeometryData> GenerateInteriorWalls(
        List<Room> rooms,
        double ceilingHeight,
        double footprintWidth,
        double footprintDepth,
        string buildingShape = "");

    /// <summary>
    /// Generate door elements with wall relationships for BIM export
    /// </summary>
    /// <param name="rooms">Room layout data</param>
    /// <param name="ceilingHeight">Height of each floor</param>
    /// <param name="footprintWidth">Total building width</param>
    /// <param name="footprintDepth">Total building depth</param>
    /// <param name="buildingShape">Building shape for boundary clipping</param>
    /// <returns>List of door elements with position data</returns>
    List<DoorElement> GenerateDoorElements(
        List<Room> rooms,
        double ceilingHeight,
        double footprintWidth,
        double footprintDepth,
        string buildingShape = "");

    /// <summary>
    /// Generate typed interior wall segments for the semantic model
    /// </summary>
    List<WallSegment> GenerateInteriorWallSegments(
        List<Room> rooms,
        double ceilingHeight,
        double footprintWidth,
        double footprintDepth,
        string buildingShape = "");

    /// <summary>
    /// Generate door elements linked to interior wall segments
    /// </summary>
    List<DoorElement> GenerateLinkedDoorElements(
        List<Room> rooms,
        double ceilingHeight,
        double footprintWidth,
        double footprintDepth,
        List<WallSegment> interiorWalls,
        string buildingShape = "");
}

/// <summary>
/// Service for generating interior walls between rooms 
/// Creates partition walls with door openings
/// </summary>
public class InteriorWallService : IInteriorWallService
{
    private readonly IGeometryService _geometryService;
    private readonly ILogger<InteriorWallService> _logger;

    // Wall and door dimensions
    private const double WallThickness = 0.5;    // 6 inches (0.5 feet)
    private const double DoorWidth = 3.0;        // 3 feet wide
    private const double DoorHeight = 7.0;       // 7 feet tall (standard door)
    private const double DoorMargin = 1.0;       // Minimum distance from wall edge
    // Inset applied to interior wall endpoints to prevent Z-fighting with exterior wall faces
    private const double WallEndInset = 0.26;    // Slightly more than WallThickness/2

    public InteriorWallService(IGeometryService geometryService, ILogger<InteriorWallService> logger)
    {
        _geometryService = geometryService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public List<GeometryData> GenerateInteriorWalls(
        List<Room> rooms,
        double ceilingHeight,
        double footprintWidth,
        double footprintDepth,
        string buildingShape = "")
    {
        var walls = new List<GeometryData>();
        
        _logger.LogInformation(
            "Generating interior walls for {RoomCount} rooms",
            rooms.Count);

        // Group rooms by floor
        var roomsByFloor = rooms.GroupBy(r => r.Floor).OrderBy(g => g.Key);

        foreach (var floorGroup in roomsByFloor)
        {
            int floor = floorGroup.Key;
            var floorRooms = floorGroup.ToList();
            double floorBaseY = (floor - 1) * ceilingHeight;

            // Find shared edges between rooms on this floor
            var sharedEdges = FindSharedEdges(floorRooms, footprintWidth, footprintDepth, buildingShape);

            foreach (var edge in sharedEdges)
            {
                var edgeWalls = GenerateWallWithDoor(
                    edge,
                    floorBaseY,
                    ceilingHeight);

                walls.AddRange(edgeWalls);
            }
        }

        _logger.LogInformation("Generated {WallCount} interior wall segments", walls.Count);
        return walls;
    }

    /// <inheritdoc/>
    public List<DoorElement> GenerateDoorElements(
        List<Room> rooms,
        double ceilingHeight,
        double footprintWidth,
        double footprintDepth,
        string buildingShape = "")
    {
        var doors = new List<DoorElement>();
        int doorIndex = 1;
        
        _logger.LogInformation(
            "Generating door elements for {RoomCount} rooms",
            rooms.Count);

        // Group rooms by floor
        var roomsByFloor = rooms.GroupBy(r => r.Floor).OrderBy(g => g.Key);

        foreach (var floorGroup in roomsByFloor)
        {
            int floor = floorGroup.Key;
            var floorRooms = floorGroup.ToList();
            double floorBaseY = (floor - 1) * ceilingHeight;

            // Find shared edges between rooms on this floor
            var sharedEdges = FindSharedEdges(floorRooms, footprintWidth, footprintDepth, buildingShape);

            foreach (var edge in sharedEdges)
            {
                // Only create door element if this edge has a door
                bool hasDoor = edge.Room1HasDoor || edge.Room2HasDoor;
                bool canFitDoor = edge.Length >= (DoorWidth + 2 * DoorMargin);
                
                if (hasDoor && canFitDoor)
                {
                    var doorElement = CreateDoorElement(edge, floor, floorBaseY, ref doorIndex);
                    doors.Add(doorElement);
                }
            }
        }

        _logger.LogInformation("Generated {DoorCount} door elements", doors.Count);
        return doors;
    }

    /// <summary>
    /// Create a door element with position and relationship data
    /// </summary>
    private DoorElement CreateDoorElement(SharedEdge edge, int floor, double floorBaseY, ref int doorIndex)
    {
        // Door is centered in the wall
        double doorCenterOffset = edge.Length / 2;
        
        double doorX, doorZ, rotationY;
        DoorWallOrientation orientation;
        
        if (edge.Orientation == EdgeOrientation.Vertical)
        {
            doorX = edge.X;
            doorZ = edge.StartZ + doorCenterOffset;
            rotationY = 0; // Door faces along X axis
            orientation = DoorWallOrientation.Vertical;
        }
        else // Horizontal
        {
            doorX = edge.StartX + doorCenterOffset;
            doorZ = edge.Z;
            rotationY = Math.PI / 2; // Door faces along Z axis
            orientation = DoorWallOrientation.Horizontal;
        }
        
        double doorCenterY = floorBaseY + (DoorHeight / 2);
        
        return new DoorElement
        {
            Id = Guid.NewGuid().ToString(),
            Name = $"Door {doorIndex++}",
            FromRoomName = edge.Room1Name,
            ToRoomName = edge.Room2Name,
            Floor = floor,
            Width = DoorWidth,
            Height = DoorHeight,
            X = doorX,
            Y = doorCenterY,
            Z = doorZ,
            RotationY = rotationY,
            IsExterior = false,
            WallOrientation = orientation,
            WallThickness = WallThickness,
            MaterialType = "wood",
            Color = "#8B4513", // Saddle brown
            OperationType = DoorOperationType.SingleSwingLeft
        };
    }

    /// <inheritdoc/>
    public List<WallSegment> GenerateInteriorWallSegments(
        List<Room> rooms,
        double ceilingHeight,
        double footprintWidth,
        double footprintDepth,
        string buildingShape = "")
    {
        var segments = new List<WallSegment>();
        var roomsByFloor = rooms.GroupBy(r => r.Floor).OrderBy(g => g.Key);

        foreach (var floorGroup in roomsByFloor)
        {
            int floor = floorGroup.Key;
            var floorRooms = floorGroup.ToList();
            double floorBaseY = (floor - 1) * ceilingHeight;
            var sharedEdges = FindSharedEdges(floorRooms, footprintWidth, footprintDepth, buildingShape);

            foreach (var edge in sharedEdges)
            {
                var segment = EdgeToWallSegment(edge, floorBaseY, ceilingHeight);
                segments.Add(segment);
            }
        }

        _logger.LogInformation("Generated {Count} interior wall segments for semantic model", segments.Count);
        return segments;
    }

    /// <inheritdoc/>
    public List<DoorElement> GenerateLinkedDoorElements(
        List<Room> rooms,
        double ceilingHeight,
        double footprintWidth,
        double footprintDepth,
        List<WallSegment> interiorWalls,
        string buildingShape = "")
    {
        var doors = new List<DoorElement>();
        int doorIndex = 1;
        var roomsByFloor = rooms.GroupBy(r => r.Floor).OrderBy(g => g.Key);

        foreach (var floorGroup in roomsByFloor)
        {
            int floor = floorGroup.Key;
            var floorRooms = floorGroup.ToList();
            double floorBaseY = (floor - 1) * ceilingHeight;
            var sharedEdges = FindSharedEdges(floorRooms, footprintWidth, footprintDepth, buildingShape);

            foreach (var edge in sharedEdges)
            {
                bool hasDoor = edge.Room1HasDoor || edge.Room2HasDoor;
                bool canFitDoor = edge.Length >= (DoorWidth + 2 * DoorMargin);

                if (hasDoor && canFitDoor)
                {
                    var doorElement = CreateDoorElement(edge, floor, floorBaseY, ref doorIndex);

                    // Link to nearest interior wall segment
                    var matched = FindNearestWallSegment(doorElement, interiorWalls);
                    if (matched != null)
                    {
                        doorElement.WallSegmentId = matched.Id;
                        // Position along wall: horizontal wall → offset on X axis, vertical → on Z axis
                        double posAlongWall = (matched.StartX == matched.EndX)
                            ? doorElement.Z - matched.StartZ
                            : doorElement.X - matched.StartX;
                        matched.Openings.Add(new Opening
                        {
                            Type = OpeningType.Door,
                            ElementId = doorElement.Id,
                            PositionAlongWall = posAlongWall,
                            Width = doorElement.Width,
                            Height = doorElement.Height,
                            SillHeight = 0
                        });
                    }

                    doors.Add(doorElement);
                }
            }
        }

        _logger.LogInformation("Generated {Count} linked door elements", doors.Count);
        return doors;
    }

    /// <summary>
    /// Convert a SharedEdge to a typed WallSegment
    /// </summary>
    private WallSegment EdgeToWallSegment(SharedEdge edge, double floorBaseY, double ceilingHeight)
    {
        double startX, startZ, endX, endZ;

        if (edge.Orientation == EdgeOrientation.Vertical)
        {
            startX = edge.X;
            startZ = edge.StartZ;
            endX = edge.X;
            endZ = edge.EndZ;
        }
        else
        {
            startX = edge.StartX;
            startZ = edge.Z;
            endX = edge.EndX;
            endZ = edge.Z;
        }

        return new WallSegment
        {
            Name = $"Interior_{edge.Room1Name}_{edge.Room2Name}_F{edge.Floor}",
            StartX = startX,
            StartZ = startZ,
            EndX = endX,
            EndZ = endZ,
            BaseY = floorBaseY,
            Height = ceilingHeight,
            Thickness = WallThickness,
            Type = WallType.Interior,
            IsLoadBearing = false,
            Floor = edge.Floor,
            MaterialType = "drywall",
            Color = "#f5f5dc"
        };
    }

    /// <summary>
    /// Find the nearest wall segment to a door by position matching
    /// </summary>
    private static WallSegment? FindNearestWallSegment(DoorElement door, List<WallSegment> walls)
    {
        WallSegment? best = null;
        double bestDist = double.MaxValue;

        foreach (var wall in walls)
        {
            if (wall.Floor != door.Floor) continue;

            // Wall midpoint
            double midX = (wall.StartX + wall.EndX) / 2.0;
            double midZ = (wall.StartZ + wall.EndZ) / 2.0;

            double dist = Math.Abs(door.X - midX) + Math.Abs(door.Z - midZ);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = wall;
            }
        }

        return best;
    }

    /// <summary>
    /// Find shared edges between adjacent rooms
    /// </summary>
    private static List<SharedEdge> FindSharedEdges(List<Room> rooms, double footprintWidth, double footprintDepth, string buildingShape = "")
    {
        var edges = new List<SharedEdge>();
        var processedPairs = new HashSet<string>();

        // L-shape boundary constants (mirrors LShapeLayoutStrategy)
        // Main wing extends to Z = +10% depth (centered). Side wing starts at X = 0 (centered).
        double lShapeMainWingMaxZ = footprintDepth * 0.1;
        double lShapeSideWingMinX = 0.0;

        // Angled layout boundary constants (mirrors AngledLayoutStrategy)
        // Main tower: 70% footprint centered at (0, 0)
        // Wing: 50% footprint centered at (+0.4*W, +0.4*D)
        double angledTowerHalfW = footprintWidth * 0.35;
        double angledTowerHalfD = footprintDepth * 0.35;
        double angledWingMinX = footprintWidth * 0.15;   // 0.4 - 0.25
        double angledWingMaxX = footprintWidth * 0.65;   // 0.4 + 0.25
        double angledWingMinZ = footprintDepth * 0.15;   // 0.4 - 0.25
        double angledWingMaxZ = footprintDepth * 0.65;   // 0.4 + 0.25

        for (int i = 0; i < rooms.Count; i++)
        {
            for (int j = i + 1; j < rooms.Count; j++)
            {
                var room1 = rooms[i];
                var room2 = rooms[j];

                // Create a unique key for this room pair
                var pairKey = $"{room1.Name}-{room2.Name}";
                if (processedPairs.Contains(pairKey)) continue;
                processedPairs.Add(pairKey);

                // Calculate room bounds in building space (centered at origin)
                var bounds1 = GetRoomBounds(room1, footprintWidth, footprintDepth);
                var bounds2 = GetRoomBounds(room2, footprintWidth, footprintDepth);

                // Check for shared edges
                var sharedEdge = FindSharedEdge(room1, room2, bounds1, bounds2);
                if (sharedEdge != null)
                {
                    // Clamp endpoints inward so interior wall faces don't coincide with
                    // the exterior building section box faces (prevents Z-fighting / poke-through)
                    double halfW = footprintWidth / 2;
                    double halfD = footprintDepth / 2;
                    if (sharedEdge.Orientation == EdgeOrientation.Vertical)
                    {
                        sharedEdge.StartZ = Math.Max(sharedEdge.StartZ, -halfD + WallEndInset);
                        sharedEdge.EndZ   = Math.Min(sharedEdge.EndZ,    halfD - WallEndInset);
                        sharedEdge.Length = sharedEdge.EndZ - sharedEdge.StartZ;

                        // L-shape: left side (X < 0) only covered by main wing; clip to main wing max Z
                        if (buildingShape == "l-shape" && sharedEdge.X < lShapeSideWingMinX)
                        {
                            sharedEdge.EndZ = Math.Min(sharedEdge.EndZ, lShapeMainWingMaxZ);
                            sharedEdge.Length = sharedEdge.EndZ - sharedEdge.StartZ;
                        }

                        // Angled: determine which section(s) cover this wall's X position
                        if (buildingShape == "angled")
                        {
                            bool inTower = sharedEdge.X >= -angledTowerHalfW && sharedEdge.X <= angledTowerHalfW;
                            bool inWing  = sharedEdge.X >= angledWingMinX && sharedEdge.X <= angledWingMaxX;

                            if (inTower && !inWing)
                            {
                                // Only in tower: clip Z to tower bounds
                                sharedEdge.StartZ = Math.Max(sharedEdge.StartZ, -angledTowerHalfD + WallEndInset);
                                sharedEdge.EndZ   = Math.Min(sharedEdge.EndZ,    angledTowerHalfD - WallEndInset);
                            }
                            else if (inWing && !inTower)
                            {
                                // Only in wing: clip Z to wing bounds
                                sharedEdge.StartZ = Math.Max(sharedEdge.StartZ, angledWingMinZ + WallEndInset);
                                sharedEdge.EndZ   = Math.Min(sharedEdge.EndZ,   angledWingMaxZ - WallEndInset);
                            }
                            else if (inTower && inWing)
                            {
                                // In overlap region: use tower bounds (wider Z coverage)
                                sharedEdge.StartZ = Math.Max(sharedEdge.StartZ, -angledTowerHalfD + WallEndInset);
                                sharedEdge.EndZ   = Math.Min(sharedEdge.EndZ,    angledTowerHalfD - WallEndInset);
                            }
                            else
                            {
                                // Not in either section: invalid wall, set length to 0 to filter out
                                sharedEdge.Length = 0;
                            }
                            if (sharedEdge.Length > 0)
                                sharedEdge.Length = sharedEdge.EndZ - sharedEdge.StartZ;
                        }
                    }
                    else
                    {
                        sharedEdge.StartX = Math.Max(sharedEdge.StartX, -halfW + WallEndInset);
                        sharedEdge.EndX   = Math.Min(sharedEdge.EndX,    halfW - WallEndInset);
                        sharedEdge.Length = sharedEdge.EndX - sharedEdge.StartX;

                        // L-shape: front area (Z > mainWingMaxZ) only covered by side wing; clip X to [0, W/2]
                        if (buildingShape == "l-shape" && sharedEdge.Z > lShapeMainWingMaxZ)
                        {
                            sharedEdge.StartX = Math.Max(sharedEdge.StartX, lShapeSideWingMinX);
                            sharedEdge.Length = sharedEdge.EndX - sharedEdge.StartX;
                        }

                        // Angled: determine which section(s) cover this wall's Z position
                        if (buildingShape == "angled")
                        {
                            bool inTower = sharedEdge.Z >= -angledTowerHalfD && sharedEdge.Z <= angledTowerHalfD;
                            bool inWing  = sharedEdge.Z >= angledWingMinZ && sharedEdge.Z <= angledWingMaxZ;

                            if (inTower && !inWing)
                            {
                                // Only in tower: clip X to tower bounds
                                sharedEdge.StartX = Math.Max(sharedEdge.StartX, -angledTowerHalfW + WallEndInset);
                                sharedEdge.EndX   = Math.Min(sharedEdge.EndX,    angledTowerHalfW - WallEndInset);
                            }
                            else if (inWing && !inTower)
                            {
                                // Only in wing: clip X to wing bounds
                                sharedEdge.StartX = Math.Max(sharedEdge.StartX, angledWingMinX + WallEndInset);
                                sharedEdge.EndX   = Math.Min(sharedEdge.EndX,   angledWingMaxX - WallEndInset);
                            }
                            else if (inTower && inWing)
                            {
                                // In overlap region: use tower bounds (wider X coverage)
                                sharedEdge.StartX = Math.Max(sharedEdge.StartX, -angledTowerHalfW + WallEndInset);
                                sharedEdge.EndX   = Math.Min(sharedEdge.EndX,    angledTowerHalfW - WallEndInset);
                            }
                            else
                            {
                                // Not in either section: invalid wall, set length to 0 to filter out
                                sharedEdge.Length = 0;
                            }
                            if (sharedEdge.Length > 0)
                                sharedEdge.Length = sharedEdge.EndX - sharedEdge.StartX;
                        }
                    }

                    if (sharedEdge.Length > 0)
                        edges.Add(sharedEdge);
                }
            }
        }

        return edges;
    }

    /// <summary>
    /// Get the bounding box of a room translated into building space (centered at origin).
    /// Rooms are defined in 0-based space (X/Z = min-corner, 0..footprint).
    /// Building geometry is centered at origin: -footprint/2..+footprint/2.
    /// </summary>
    private static RoomBounds GetRoomBounds(Room room, double footprintWidth, double footprintDepth)
    {
        double offsetX = footprintWidth / 2;
        double offsetZ = footprintDepth / 2;
        return new RoomBounds
        {
            MinX = room.X - offsetX,
            MaxX = room.X + room.Width - offsetX,
            MinZ = room.Z - offsetZ,
            MaxZ = room.Z + room.Depth - offsetZ
        };
    }

    /// <summary>
    /// Find the shared edge between two adjacent rooms
    /// </summary>
    private static SharedEdge? FindSharedEdge(Room room1, Room room2, RoomBounds bounds1, RoomBounds bounds2)
    {
        const double tolerance = 0.1;

        // Check if rooms share a vertical edge (along Z axis)
        // Room1 right edge touches Room2 left edge
        if (Math.Abs(bounds1.MaxX - bounds2.MinX) < tolerance)
        {
            double overlapMinZ = Math.Max(bounds1.MinZ, bounds2.MinZ);
            double overlapMaxZ = Math.Min(bounds1.MaxZ, bounds2.MaxZ);
            
            if (overlapMaxZ > overlapMinZ)
            {
                return new SharedEdge
                {
                    Orientation = EdgeOrientation.Vertical,
                    X = bounds1.MaxX,
                    StartZ = overlapMinZ,
                    EndZ = overlapMaxZ,
                    Length = overlapMaxZ - overlapMinZ,
                    Room1HasDoor = room1.HasDoor,
                    Room2HasDoor = room2.HasDoor,
                    Room1Name = room1.Name,
                    Room2Name = room2.Name,
                    Floor = room1.Floor
                };
            }
        }

        // Room1 left edge touches Room2 right edge
        if (Math.Abs(bounds1.MinX - bounds2.MaxX) < tolerance)
        {
            double overlapMinZ = Math.Max(bounds1.MinZ, bounds2.MinZ);
            double overlapMaxZ = Math.Min(bounds1.MaxZ, bounds2.MaxZ);
            
            if (overlapMaxZ > overlapMinZ)
            {
                return new SharedEdge
                {
                    Orientation = EdgeOrientation.Vertical,
                    X = bounds1.MinX,
                    StartZ = overlapMinZ,
                    EndZ = overlapMaxZ,
                    Length = overlapMaxZ - overlapMinZ,
                    Room1HasDoor = room1.HasDoor,
                    Room2HasDoor = room2.HasDoor,
                    Room1Name = room1.Name,
                    Room2Name = room2.Name,
                    Floor = room1.Floor
                };
            }
        }

        // Check if rooms share a horizontal edge (along X axis)
        // Room1 front edge touches Room2 back edge
        if (Math.Abs(bounds1.MaxZ - bounds2.MinZ) < tolerance)
        {
            double overlapMinX = Math.Max(bounds1.MinX, bounds2.MinX);
            double overlapMaxX = Math.Min(bounds1.MaxX, bounds2.MaxX);
            
            if (overlapMaxX > overlapMinX)
            {
                return new SharedEdge
                {
                    Orientation = EdgeOrientation.Horizontal,
                    Z = bounds1.MaxZ,
                    StartX = overlapMinX,
                    EndX = overlapMaxX,
                    Length = overlapMaxX - overlapMinX,
                    Room1HasDoor = room1.HasDoor,
                    Room2HasDoor = room2.HasDoor,
                    Room1Name = room1.Name,
                    Room2Name = room2.Name,
                    Floor = room1.Floor
                };
            }
        }

        // Room1 back edge touches Room2 front edge
        if (Math.Abs(bounds1.MinZ - bounds2.MaxZ) < tolerance)
        {
            double overlapMinX = Math.Max(bounds1.MinX, bounds2.MinX);
            double overlapMaxX = Math.Min(bounds1.MaxX, bounds2.MaxX);
            
            if (overlapMaxX > overlapMinX)
            {
                return new SharedEdge
                {
                    Orientation = EdgeOrientation.Horizontal,
                    Z = bounds1.MinZ,
                    StartX = overlapMinX,
                    EndX = overlapMaxX,
                    Length = overlapMaxX - overlapMinX,
                    Room1HasDoor = room1.HasDoor,
                    Room2HasDoor = room2.HasDoor,
                    Room1Name = room1.Name,
                    Room2Name = room2.Name,
                    Floor = room1.Floor
                };
            }
        }

        return null;
    }

    /// <summary>
    /// Generate wall segments with optional door opening
    /// </summary>
    private List<GeometryData> GenerateWallWithDoor(
        SharedEdge edge,
        double floorBaseY,
        double ceilingHeight)
    {
        var walls = new List<GeometryData>();
        
        bool hasDoor = edge.Room1HasDoor || edge.Room2HasDoor;
        bool canFitDoor = edge.Length >= (DoorWidth + 2 * DoorMargin);

        if (hasDoor && canFitDoor)
        {
            // Generate wall with door opening (3 segments: left, above door, right)
            walls.AddRange(GenerateWallWithDoorOpening(edge, floorBaseY, ceilingHeight));
        }
        else
        {
            // Generate solid wall
            walls.Add(GenerateSolidWall(edge, floorBaseY, ceilingHeight));
        }

        return walls;
    }

    /// <summary>
    /// Generate a wall with a door opening (left segment, header, right segment)
    /// </summary>
    private List<GeometryData> GenerateWallWithDoorOpening(
        SharedEdge edge,
        double floorBaseY,
        double ceilingHeight)
    {
        var walls = new List<GeometryData>();
        
        // Door is centered in the wall
        double doorCenterOffset = edge.Length / 2;
        double leftSegmentLength = doorCenterOffset - (DoorWidth / 2);
        double rightSegmentLength = edge.Length - doorCenterOffset - (DoorWidth / 2);
        double headerHeight = ceilingHeight - DoorHeight;

        if (edge.Orientation == EdgeOrientation.Vertical)
        {
            double wallX = edge.X;
            double doorCenterZ = edge.StartZ + doorCenterOffset;

            // Left segment (below door start)
            if (leftSegmentLength > 0.1)
            {
                double segmentCenterZ = edge.StartZ + leftSegmentLength / 2;
                walls.Add(CreateVerticalWallSegment(
                    wallX, segmentCenterZ, leftSegmentLength,
                    floorBaseY, ceilingHeight));
            }

            // Right segment (after door)
            if (rightSegmentLength > 0.1)
            {
                double segmentCenterZ = edge.EndZ - rightSegmentLength / 2;
                walls.Add(CreateVerticalWallSegment(
                    wallX, segmentCenterZ, rightSegmentLength,
                    floorBaseY, ceilingHeight));
            }

            // Header above door
            if (headerHeight > 0.1)
            {
                walls.Add(CreateVerticalWallSegment(
                    wallX, doorCenterZ, DoorWidth,
                    floorBaseY + DoorHeight, headerHeight));
            }
        }
        else // Horizontal wall
        {
            double wallZ = edge.Z;
            double doorCenterX = edge.StartX + doorCenterOffset;

            // Left segment
            if (leftSegmentLength > 0.1)
            {
                double segmentCenterX = edge.StartX + leftSegmentLength / 2;
                walls.Add(CreateHorizontalWallSegment(
                    segmentCenterX, wallZ, leftSegmentLength,
                    floorBaseY, ceilingHeight));
            }

            // Right segment
            if (rightSegmentLength > 0.1)
            {
                double segmentCenterX = edge.EndX - rightSegmentLength / 2;
                walls.Add(CreateHorizontalWallSegment(
                    segmentCenterX, wallZ, rightSegmentLength,
                    floorBaseY, ceilingHeight));
            }

            // Header above door
            if (headerHeight > 0.1)
            {
                walls.Add(CreateHorizontalWallSegment(
                    doorCenterX, wallZ, DoorWidth,
                    floorBaseY + DoorHeight, headerHeight));
            }
        }

        return walls;
    }

    /// <summary>
    /// Generate a solid wall (no door opening)
    /// </summary>
    private GeometryData GenerateSolidWall(
        SharedEdge edge,
        double floorBaseY,
        double ceilingHeight)
    {
        if (edge.Orientation == EdgeOrientation.Vertical)
        {
            double centerZ = (edge.StartZ + edge.EndZ) / 2;
            return CreateVerticalWallSegment(edge.X, centerZ, edge.Length, floorBaseY, ceilingHeight);
        }
        else
        {
            double centerX = (edge.StartX + edge.EndX) / 2;
            return CreateHorizontalWallSegment(centerX, edge.Z, edge.Length, floorBaseY, ceilingHeight);
        }
    }

    /// <summary>
    /// Create a vertical wall segment (runs along Z axis)
    /// </summary>
    private GeometryData CreateVerticalWallSegment(
        double x, double centerZ, double length,
        double baseY, double height)
    {
        double centerY = baseY + height / 2;
        
        return _geometryService.CreateBox(
            WallThickness,  // Width (thin)
            height,         // Height
            length,         // Depth (along Z)
            x,
            centerY,
            centerZ,
            "drywall",
            "#f5f5dc"       // Beige
        );
    }

    /// <summary>
    /// Create a horizontal wall segment (runs along X axis)
    /// </summary>
    private GeometryData CreateHorizontalWallSegment(
        double centerX, double z, double length,
        double baseY, double height)
    {
        double centerY = baseY + height / 2;
        
        return _geometryService.CreateBox(
            length,         // Width (along X)
            height,         // Height
            WallThickness,  // Depth (thin)
            centerX,
            centerY,
            z,
            "drywall",
            "#f5f5dc"       // Beige
        );
    }

    /// <summary>
    /// Room bounding box helper
    /// </summary>
    private class RoomBounds
    {
        public double MinX { get; set; }
        public double MaxX { get; set; }
        public double MinZ { get; set; }
        public double MaxZ { get; set; }
    }

    /// <summary>
    /// Shared edge between two rooms
    /// </summary>
    private class SharedEdge
    {
        public EdgeOrientation Orientation { get; set; }
        public double X { get; set; }        // For vertical walls
        public double Z { get; set; }        // For horizontal walls
        public double StartX { get; set; }
        public double EndX { get; set; }
        public double StartZ { get; set; }
        public double EndZ { get; set; }
        public double Length { get; set; }
        public bool Room1HasDoor { get; set; }
        public bool Room2HasDoor { get; set; }
        public string Room1Name { get; set; } = string.Empty;
        public string Room2Name { get; set; } = string.Empty;
        public int Floor { get; set; }
    }

    private enum EdgeOrientation
    {
        Horizontal,  // Wall runs along X axis
        Vertical     // Wall runs along Z axis
    }
}
