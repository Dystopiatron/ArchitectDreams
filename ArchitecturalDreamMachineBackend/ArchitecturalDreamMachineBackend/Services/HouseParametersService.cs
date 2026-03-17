using ArchitecturalDreamMachineBackend.Constants;
using ArchitecturalDreamMachineBackend.Data;
using ArchitecturalDreamMachineBackend.Geometry;

namespace ArchitecturalDreamMachineBackend.Services;

/// <summary>
/// Interface for generating HouseParameters from design inputs
/// </summary>
public interface IHouseParametersService
{
    /// <summary>
    /// Calculate HouseParameters from lot size, style template, and optional overrides
    /// </summary>
    /// <param name="lotSize">Lot size in square feet</param>
    /// <param name="styleTemplate">Style template with architectural defaults</param>
    /// <param name="buildingShapeOverride">Optional override for building shape</param>
    /// <param name="storiesOverride">Optional override for number of stories</param>
    /// <returns>Complete HouseParameters ready for geometry generation</returns>
    HouseParameters CalculateParameters(
        double lotSize,
        StyleTemplate styleTemplate,
        string? buildingShapeOverride = null,
        int? storiesOverride = null);
}

/// <summary>
/// Service for generating HouseParameters from design inputs
/// Consolidates all parameter calculation logic in one place
/// </summary>
public class HouseParametersService : IHouseParametersService
{
    private readonly ILogger<HouseParametersService> _logger;

    public HouseParametersService(ILogger<HouseParametersService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public HouseParameters CalculateParameters(
        double lotSize,
        StyleTemplate styleTemplate,
        string? buildingShapeOverride = null,
        int? storiesOverride = null)
    {
        _logger.LogInformation(
            "Calculating parameters: lotSize={LotSize}, style={Style}, shapeOverride={Shape}, storiesOverride={Stories}",
            lotSize, styleTemplate.Name, buildingShapeOverride, storiesOverride);

        // Calculate architectural dimensions
        var desiredBuildingSqFt = lotSize;
        // Use override if provided, otherwise use style template default
        var stories = storiesOverride ?? styleTemplate.TypicalStories;
        var footprintSqFt = desiredBuildingSqFt / stories;

        // Rectangular footprint (1.5:1 aspect ratio)
        var footprintWidth = Math.Sqrt(footprintSqFt / ArchitecturalConstants.DefaultAspectRatio);
        var footprintDepth = footprintSqFt / footprintWidth;

        // Determine building shape
        var buildingShape = buildingShapeOverride ?? styleTemplate.BuildingShape;

        // Generate room layout (pass style's window ratio for proper differentiation)
        var rooms = GenerateRoomLayout(
            footprintWidth,
            footprintDepth,
            styleTemplate.RoomCount,
            stories,
            buildingShape,
            styleTemplate.WindowToWallRatio
        );

        // Create HouseParameters
        var houseParameters = new HouseParameters
        {
            LotSize = lotSize,
            RoofType = styleTemplate.RoofType,
            WindowStyle = styleTemplate.WindowStyle,
            RoomCount = styleTemplate.RoomCount,
            Material = new Material
            {
                Color = styleTemplate.Color,
                Texture = styleTemplate.Texture
            },
            // Architectural parameters
            CeilingHeight = styleTemplate.TypicalCeilingHeight,
            Stories = stories,
            BuildingShape = buildingShape,
            WindowToWallRatio = styleTemplate.WindowToWallRatio,
            FoundationType = styleTemplate.FoundationType,
            ExteriorMaterial = styleTemplate.ExteriorMaterial,
            FootprintWidth = footprintWidth,
            FootprintDepth = footprintDepth,
            RoofPitch = styleTemplate.RoofPitch,
            HasParapet = styleTemplate.HasParapet,
            HasEaves = styleTemplate.HasEaves,
            EavesOverhang = styleTemplate.EavesOverhang,
            Rooms = rooms
        };

        _logger.LogInformation(
            "Parameters calculated: {Width}x{Depth}, {Stories} stories, {Rooms} rooms",
            footprintWidth, footprintDepth, stories, rooms.Count);

        return houseParameters;
    }

    /// <summary>
    /// Generate room layout based on building dimensions and room count
    /// </summary>
    /// <param name="windowToWallRatio">Style-specific window-to-wall ratio (e.g., 0.10 brutalist, 0.30 modern)</param>
    private List<Room> GenerateRoomLayout(
        double width,
        double depth,
        int roomCount,
        int stories,
        string shape,
        double windowToWallRatio)
    {
        var rooms = new List<Room>();

        // L-shape requires rooms constrained to the two-wing footprint so that windows
        // and interior walls don't appear in the void (front-right) quadrant.
        if (shape == "l-shape")
            return GenerateLShapeRoomLayout(width, depth, roomCount, stories);

        // Angled layout has a wing at (+0.4W, +0.4D) that extends beyond the main footprint.
        // Standard room generation doesn't place rooms there, so we need custom layout.
        if (shape == "angled")
            return GenerateAngledRoomLayout(width, depth, roomCount, stories);

        // Split-level: floor 1 spans full footprint, floor 2 only spans right half (main section)
        if (shape == "split-level")
            return GenerateSplitLevelRoomLayout(width, depth, roomCount, stories);

        if (roomCount <= 3)
        {
            // Small house: living, bedroom, bath
            rooms.Add(new Room
            {
                Name = "Living Room",
                Floor = 1,
                X = 0,
                Z = 0,
                Width = width,
                Depth = depth * 0.6,
                WindowCount = CalculateWindowCount(width, depth * 0.6, windowToWallRatio),
                HasDoor = true
            });
            rooms.Add(new Room
            {
                Name = "Bedroom",
                Floor = 1,
                X = 0,
                Z = depth * 0.6,
                Width = width * 0.6,
                Depth = depth * 0.4,
                WindowCount = 1, // Egress window required
                HasDoor = true
            });
            rooms.Add(new Room
            {
                Name = "Bathroom",
                Floor = 1,
                X = width * 0.6,
                Z = depth * 0.6,
                Width = width * 0.4,
                Depth = depth * 0.4,
                WindowCount = 0,
                HasDoor = true
            });
        }
        else if (roomCount <= 5)
        {
            // Medium house
            if (stories == 1)
            {
                // Single-story layout
                rooms.Add(new Room
                {
                    Name = "Living Room",
                    Floor = 1,
                    X = 0,
                    Z = 0,
                    Width = width * 0.6,
                    Depth = depth * 0.5,
                    WindowCount = CalculateWindowCount(width * 0.6, depth * 0.5, windowToWallRatio),
                    HasDoor = true
                });
                rooms.Add(new Room
                {
                    Name = "Kitchen",
                    Floor = 1,
                    X = width * 0.6,
                    Z = 0,
                    Width = width * 0.4,
                    Depth = depth * 0.5,
                    WindowCount = 1,
                    HasDoor = true
                });
                rooms.Add(new Room
                {
                    Name = "Bedroom 1",
                    Floor = 1,
                    X = 0,
                    Z = depth * 0.5,
                    Width = width * 0.5,
                    Depth = depth * 0.5,
                    WindowCount = 1,
                    HasDoor = true
                });
                rooms.Add(new Room
                {
                    Name = "Bedroom 2",
                    Floor = 1,
                    X = width * 0.5,
                    Z = depth * 0.5,
                    Width = width * 0.3,
                    Depth = depth * 0.5,
                    WindowCount = 1,
                    HasDoor = true
                });
                rooms.Add(new Room
                {
                    Name = "Bathroom",
                    Floor = 1,
                    X = width * 0.8,
                    Z = depth * 0.5,
                    Width = width * 0.2,
                    Depth = depth * 0.5,
                    WindowCount = 0,
                    HasDoor = true
                });
            }
            else
            {
                // Two-story layout
                // First floor: living, kitchen, powder room
                rooms.Add(new Room
                {
                    Name = "Living Room",
                    Floor = 1,
                    X = 0,
                    Z = 0,
                    Width = width * 0.6,
                    Depth = depth * 0.7,
                    WindowCount = CalculateWindowCount(width * 0.6, depth * 0.7, windowToWallRatio),
                    HasDoor = true
                });
                rooms.Add(new Room
                {
                    Name = "Kitchen",
                    Floor = 1,
                    X = width * 0.6,
                    Z = 0,
                    Width = width * 0.4,
                    Depth = depth * 0.7,
                    WindowCount = 2,
                    HasDoor = true
                });
                rooms.Add(new Room
                {
                    Name = "Powder Room",
                    Floor = 1,
                    X = 0,
                    Z = depth * 0.7,
                    Width = width,
                    Depth = depth * 0.3,
                    WindowCount = 0,
                    HasDoor = true
                });

                // Second floor: bedrooms, bath
                rooms.Add(new Room
                {
                    Name = "Master Bedroom",
                    Floor = 2,
                    X = 0,
                    Z = 0,
                    Width = width * 0.5,
                    Depth = depth * 0.6,
                    WindowCount = 2,
                    HasDoor = true
                });
                rooms.Add(new Room
                {
                    Name = "Bedroom 2",
                    Floor = 2,
                    X = width * 0.5,
                    Z = 0,
                    Width = width * 0.5,
                    Depth = depth * 0.6,
                    WindowCount = 1,
                    HasDoor = true
                });
                rooms.Add(new Room
                {
                    Name = "Bathroom",
                    Floor = 2,
                    X = 0,
                    Z = depth * 0.6,
                    Width = width,
                    Depth = depth * 0.4,
                    WindowCount = 0,
                    HasDoor = true
                });
            }
        }
        else // 6+ rooms
        {
            // Large house - two story recommended
            if (stories >= 2)
            {
                // First floor
                rooms.Add(new Room
                {
                    Name = "Living Room",
                    Floor = 1,
                    X = 0,
                    Z = 0,
                    Width = width * 0.5,
                    Depth = depth * 0.5,
                    WindowCount = CalculateWindowCount(width * 0.5, depth * 0.5, windowToWallRatio),
                    HasDoor = true
                });
                rooms.Add(new Room
                {
                    Name = "Dining Room",
                    Floor = 1,
                    X = width * 0.5,
                    Z = 0,
                    Width = width * 0.5,
                    Depth = depth * 0.5,
                    WindowCount = 2,
                    HasDoor = true
                });
                rooms.Add(new Room
                {
                    Name = "Kitchen",
                    Floor = 1,
                    X = 0,
                    Z = depth * 0.5,
                    Width = width * 0.6,
                    Depth = depth * 0.5,
                    WindowCount = 2,
                    HasDoor = true
                });
                rooms.Add(new Room
                {
                    Name = "Powder Room",
                    Floor = 1,
                    X = width * 0.6,
                    Z = depth * 0.5,
                    Width = width * 0.4,
                    Depth = depth * 0.5,
                    WindowCount = 0,
                    HasDoor = true
                });

                // Second floor
                rooms.Add(new Room
                {
                    Name = "Master Bedroom",
                    Floor = 2,
                    X = 0,
                    Z = 0,
                    Width = width * 0.4,
                    Depth = depth * 0.5,
                    WindowCount = 2,
                    HasDoor = true
                });
                rooms.Add(new Room
                {
                    Name = "Bedroom 2",
                    Floor = 2,
                    X = width * 0.4,
                    Z = 0,
                    Width = width * 0.3,
                    Depth = depth * 0.5,
                    WindowCount = 1,
                    HasDoor = true
                });
                rooms.Add(new Room
                {
                    Name = "Bedroom 3",
                    Floor = 2,
                    X = width * 0.7,
                    Z = 0,
                    Width = width * 0.3,
                    Depth = depth * 0.5,
                    WindowCount = 1,
                    HasDoor = true
                });
                rooms.Add(new Room
                {
                    Name = "Master Bath",
                    Floor = 2,
                    X = 0,
                    Z = depth * 0.5,
                    Width = width * 0.4,
                    Depth = depth * 0.5,
                    WindowCount = 0,
                    HasDoor = true
                });
                rooms.Add(new Room
                {
                    Name = "Bathroom",
                    Floor = 2,
                    X = width * 0.4,
                    Z = depth * 0.5,
                    Width = width * 0.6,
                    Depth = depth * 0.5,
                    WindowCount = 0,
                    HasDoor = true
                });
            }
            else
            {
                // Single story large house - simplified
                double roomWidth = width / 3;
                double roomDepth = depth / 2;

                for (int i = 0; i < roomCount; i++)
                {
                    string roomName = i == 0 ? "Living Room" :
                                     i == 1 ? "Kitchen" :
                                     i == roomCount - 1 ? "Bathroom" :
                                     $"Bedroom {i - 1}";

                    bool isBathroom = roomName.Contains("Bathroom");

                    rooms.Add(new Room
                    {
                        Name = roomName,
                        Floor = 1,
                        X = (i % 3) * roomWidth,
                        Z = (i / 3) * roomDepth,
                        Width = roomWidth,
                        Depth = roomDepth,
                        WindowCount = isBathroom ? 0 : 1,
                        HasDoor = true
                    });
                }
            }
        }

        // Ensure every story from 3 upward has rooms so WindowService generates
        // windows on all levels (it iterates by room floor group).
        var maxFloor = rooms.Any() ? rooms.Max(r => r.Floor) : 1;
        for (int floor = maxFloor + 1; floor <= stories; floor++)
        {
            rooms.Add(new Room { Name = $"Upper Bedroom {floor - 1}", Floor = floor,
                X = 0, Z = 0, Width = width * 0.5, Depth = depth * 0.6,
                WindowCount = 2, HasDoor = true });
            rooms.Add(new Room { Name = $"Upper Room {floor - 1}", Floor = floor,
                X = width * 0.5, Z = 0, Width = width * 0.5, Depth = depth * 0.6,
                WindowCount = 1, HasDoor = true });
        }

        return rooms;
    }

    /// <summary>
    /// Generate rooms constrained to the L-shape footprint.
    /// Main wing:   X ∈ [0, W],     Z ∈ [0, 0.6D]  (full width, back 60%)
    /// Corner wing: X ∈ [0, 0.5W],  Z ∈ [0.6D, D]  (left half, front 40%)
    /// No room spans into the void (front-right quadrant).
    /// </summary>
    private static List<Room> GenerateLShapeRoomLayout(
        double width, double depth, int roomCount, int stories)
    {
        double backDepth   = depth * 0.6;  // main wing Z depth
        double frontDepth  = depth * 0.4;  // corner wing Z depth
        double cornerWidth = width * 0.5;  // corner wing width

        var rooms = new List<Room>();

        if (roomCount <= 3 || stories == 1)
        {
            // Two rooms in main wing, one or more in corner wing
            rooms.Add(new Room { Name = "Living Room", Floor = 1,
                X = 0, Z = 0, Width = cornerWidth, Depth = backDepth,
                WindowCount = 2, HasDoor = true });
            rooms.Add(new Room { Name = "Kitchen", Floor = 1,
                X = cornerWidth, Z = 0, Width = cornerWidth, Depth = backDepth,
                WindowCount = 3, HasDoor = true });
            rooms.Add(new Room { Name = "Bedroom", Floor = 1,
                X = 0, Z = backDepth, Width = cornerWidth * 0.5, Depth = frontDepth,
                WindowCount = 1, HasDoor = true });
            if (roomCount >= 4)
                rooms.Add(new Room { Name = "Bathroom", Floor = 1,
                    X = cornerWidth * 0.5, Z = backDepth,
                    Width = cornerWidth * 0.5, Depth = frontDepth,
                    WindowCount = 0, HasDoor = true });
        }
        else if (roomCount <= 5)
        {
            // Floor 1: 2 main-wing rooms + 1 corner-wing room
            rooms.Add(new Room { Name = "Living Room", Floor = 1,
                X = 0, Z = 0, Width = cornerWidth, Depth = backDepth,
                WindowCount = 2, HasDoor = true });
            rooms.Add(new Room { Name = "Kitchen", Floor = 1,
                X = cornerWidth, Z = 0, Width = cornerWidth, Depth = backDepth,
                WindowCount = 3, HasDoor = true });
            rooms.Add(new Room { Name = "Powder Room", Floor = 1,
                X = 0, Z = backDepth, Width = cornerWidth, Depth = frontDepth,
                WindowCount = 0, HasDoor = true });
            // Floor 2: 2 bedrooms in main wing + 1 study in corner wing
            rooms.Add(new Room { Name = "Master Bedroom", Floor = 2,
                X = 0, Z = 0, Width = cornerWidth, Depth = backDepth,
                WindowCount = 2, HasDoor = true });
            rooms.Add(new Room { Name = "Bedroom 2", Floor = 2,
                X = cornerWidth, Z = 0, Width = cornerWidth, Depth = backDepth,
                WindowCount = 3, HasDoor = true });
            rooms.Add(new Room { Name = "Study", Floor = 2,
                X = 0, Z = backDepth, Width = cornerWidth, Depth = frontDepth,
                WindowCount = 3, HasDoor = true });
        }
        else
        {
            // 6+ rooms: 4 on Floor 1, 4 on Floor 2
            double mainThird = width / 3;
            // Floor 1: 3 main-wing rooms + 1 corner-wing room
            rooms.Add(new Room { Name = "Living Room", Floor = 1,
                X = 0, Z = 0, Width = mainThird, Depth = backDepth,
                WindowCount = 2, HasDoor = true });
            rooms.Add(new Room { Name = "Dining Room", Floor = 1,
                X = mainThird, Z = 0, Width = mainThird, Depth = backDepth,
                WindowCount = 1, HasDoor = true });
            rooms.Add(new Room { Name = "Kitchen", Floor = 1,
                X = mainThird * 2, Z = 0, Width = mainThird, Depth = backDepth,
                WindowCount = 3, HasDoor = true });
            rooms.Add(new Room { Name = "Entry", Floor = 1,
                X = 0, Z = backDepth, Width = cornerWidth, Depth = frontDepth,
                WindowCount = 3, HasDoor = true });
            // Floor 2: 2 main-wing bedrooms + 2 corner-wing rooms
            rooms.Add(new Room { Name = "Master Bedroom", Floor = 2,
                X = 0, Z = 0, Width = cornerWidth, Depth = backDepth,
                WindowCount = 2, HasDoor = true });
            rooms.Add(new Room { Name = "Bedroom 2", Floor = 2,
                X = cornerWidth, Z = 0, Width = cornerWidth, Depth = backDepth,
                WindowCount = 3, HasDoor = true });
            rooms.Add(new Room { Name = "Bedroom 3", Floor = 2,
                X = 0, Z = backDepth, Width = cornerWidth * 0.5, Depth = frontDepth,
                WindowCount = 1, HasDoor = true });
            rooms.Add(new Room { Name = "Bathroom", Floor = 2,
                X = cornerWidth * 0.5, Z = backDepth,
                Width = cornerWidth * 0.5, Depth = frontDepth,
                WindowCount = 2, HasDoor = true });
        }

        // Ensure every story from 3 upward has rooms in both wings so WindowService
        // generates windows on all levels and all exterior faces (including step walls).
        var maxFloor = rooms.Any() ? rooms.Max(r => r.Floor) : 1;
        for (int floor = maxFloor + 1; floor <= stories; floor++)
        {
            // Main wing — left half (Back + Left walls)
            rooms.Add(new Room { Name = $"Upper Bedroom {floor - 1}", Floor = floor,
                X = 0, Z = 0, Width = cornerWidth, Depth = backDepth,
                WindowCount = 2, HasDoor = true });
            // Main wing — right half (Back + Right + Step-front walls → 3 windows)
            rooms.Add(new Room { Name = $"Upper Room {floor - 1}", Floor = floor,
                X = cornerWidth, Z = 0, Width = cornerWidth, Depth = backDepth,
                WindowCount = 3, HasDoor = true });
            // Corner wing (Front + Left + Step-right walls → 3 windows)
            rooms.Add(new Room { Name = $"Upper Corner {floor - 1}", Floor = floor,
                X = 0, Z = backDepth, Width = cornerWidth, Depth = frontDepth,
                WindowCount = 3, HasDoor = true });
        }

        return rooms;
    }

    /// <summary>
    /// Generate room layout for angled buildings.
    /// The angled layout has a main tower (70% at center) and a wing (50% at +0.4W, +0.4D).
    /// The wing extends beyond the main footprint, so we need a dedicated room for windows.
    /// </summary>
    private static List<Room> GenerateAngledRoomLayout(
        double width, double depth, int roomCount, int stories)
    {
        // Main tower: 70% of footprint, centered at origin
        double towerW = width * 0.7;
        double towerD = depth * 0.7;
        // In 0-based room coords, the tower spans from 0.15*footprint to 0.85*footprint
        double towerMinX = (width - towerW) / 2;   // 0.15W
        double towerMinZ = (depth - towerD) / 2;   // 0.15D

        // Wing: 50% of footprint, offset at (+0.4W, +0.4D) building coords
        // In 0-based room coords: center at (0.9W, 0.9D), size 50%
        // The wing extends from 0.65W to 1.15W, but we can only define rooms within 0..footprint.
        // So we create a room at the corner (0.65W to W, 0.65D to D) which will be clamped to wing.
        double wingRoomX = width * 0.65;
        double wingRoomZ = depth * 0.65;
        double wingRoomW = width * 0.35;  // Goes to edge of footprint
        double wingRoomD = depth * 0.35;

        var rooms = new List<Room>();

        if (stories == 1 || roomCount <= 3)
        {
            // Simple layout: main room in tower + sunroom in wing
            rooms.Add(new Room { Name = "Living Room", Floor = 1,
                X = towerMinX, Z = towerMinZ, Width = towerW * 0.6, Depth = towerD,
                WindowCount = 3, HasDoor = true });
            rooms.Add(new Room { Name = "Kitchen", Floor = 1,
                X = towerMinX + towerW * 0.6, Z = towerMinZ, Width = towerW * 0.4, Depth = towerD * 0.6,
                WindowCount = 2, HasDoor = true });
            rooms.Add(new Room { Name = "Bedroom", Floor = 1,
                X = towerMinX + towerW * 0.6, Z = towerMinZ + towerD * 0.6, Width = towerW * 0.4, Depth = towerD * 0.4,
                WindowCount = 1, HasDoor = true });
            // Wing sunroom - positioned to fill the wing section
            rooms.Add(new Room { Name = "Sunroom", Floor = 1,
                X = wingRoomX, Z = wingRoomZ, Width = wingRoomW, Depth = wingRoomD,
                WindowCount = 4, HasDoor = true });
        }
        else
        {
            // Multi-story: rooms in tower for all floors + wing on floor 1
            // Floor 1
            rooms.Add(new Room { Name = "Living Room", Floor = 1,
                X = towerMinX, Z = towerMinZ, Width = towerW * 0.6, Depth = towerD * 0.7,
                WindowCount = 3, HasDoor = true });
            rooms.Add(new Room { Name = "Kitchen", Floor = 1,
                X = towerMinX + towerW * 0.6, Z = towerMinZ, Width = towerW * 0.4, Depth = towerD * 0.7,
                WindowCount = 2, HasDoor = true });
            rooms.Add(new Room { Name = "Powder Room", Floor = 1,
                X = towerMinX, Z = towerMinZ + towerD * 0.7, Width = towerW, Depth = towerD * 0.3,
                WindowCount = 1, HasDoor = true });
            // Wing sunroom
            rooms.Add(new Room { Name = "Sunroom", Floor = 1,
                X = wingRoomX, Z = wingRoomZ, Width = wingRoomW, Depth = wingRoomD,
                WindowCount = 4, HasDoor = true });

            // Floor 2
            rooms.Add(new Room { Name = "Master Bedroom", Floor = 2,
                X = towerMinX, Z = towerMinZ, Width = towerW * 0.5, Depth = towerD * 0.6,
                WindowCount = 2, HasDoor = true });
            rooms.Add(new Room { Name = "Bedroom 2", Floor = 2,
                X = towerMinX + towerW * 0.5, Z = towerMinZ, Width = towerW * 0.5, Depth = towerD * 0.6,
                WindowCount = 2, HasDoor = true });
            rooms.Add(new Room { Name = "Bathroom", Floor = 2,
                X = towerMinX, Z = towerMinZ + towerD * 0.6, Width = towerW * 0.4, Depth = towerD * 0.4,
                WindowCount = 1, HasDoor = true });
            rooms.Add(new Room { Name = "Study", Floor = 2,
                X = towerMinX + towerW * 0.4, Z = towerMinZ + towerD * 0.6, Width = towerW * 0.6, Depth = towerD * 0.4,
                WindowCount = 2, HasDoor = true });
        }

        // Upper floors (3+): bedrooms spanning the tower only
        var maxFloor = rooms.Any() ? rooms.Max(r => r.Floor) : 1;
        for (int floor = maxFloor + 1; floor <= stories; floor++)
        {
            rooms.Add(new Room { Name = $"Upper Bedroom {floor - 1}", Floor = floor,
                X = towerMinX, Z = towerMinZ, Width = towerW * 0.5, Depth = towerD,
                WindowCount = 3, HasDoor = true });
            rooms.Add(new Room { Name = $"Upper Room {floor - 1}", Floor = floor,
                X = towerMinX + towerW * 0.5, Z = towerMinZ, Width = towerW * 0.5, Depth = towerD,
                WindowCount = 3, HasDoor = true });
        }

        return rooms;
    }

    /// <summary>
    /// Generate room layout for split-level buildings.
    /// Floor 1: Full footprint (wing on left 50%, main on right 50%)
    /// Floor 2: Only main section (right 50% of footprint)
    /// </summary>
    private static List<Room> GenerateSplitLevelRoomLayout(
        double width, double depth, int roomCount, int stories)
    {
        var rooms = new List<Room>();
        
        // Main section is right 50% of footprint
        // In 0-based room coords, X from width*0.5 to width
        double mainMinX = width * 0.5;
        double mainW = width * 0.5;
        
        // Wing section is left 50% of footprint
        // In 0-based room coords, X from 0 to width*0.5
        double wingMinX = 0;
        double wingW = width * 0.5;
        
        // Floor 1: rooms in both sections
        // Wing (left side) - garage/family room
        rooms.Add(new Room { Name = "Family Room", Floor = 1,
            X = wingMinX, Z = 0, Width = wingW, Depth = depth * 0.6,
            WindowCount = 2, HasDoor = true });
        rooms.Add(new Room { Name = "Garage", Floor = 1,
            X = wingMinX, Z = depth * 0.6, Width = wingW, Depth = depth * 0.4,
            WindowCount = 1, HasDoor = true });
        
        // Main (right side) - living areas
        rooms.Add(new Room { Name = "Living Room", Floor = 1,
            X = mainMinX, Z = 0, Width = mainW, Depth = depth * 0.5,
            WindowCount = 3, HasDoor = true });
        rooms.Add(new Room { Name = "Kitchen", Floor = 1,
            X = mainMinX, Z = depth * 0.5, Width = mainW * 0.6, Depth = depth * 0.5,
            WindowCount = 2, HasDoor = true });
        rooms.Add(new Room { Name = "Dining", Floor = 1,
            X = mainMinX + mainW * 0.6, Z = depth * 0.5, Width = mainW * 0.4, Depth = depth * 0.5,
            WindowCount = 1, HasDoor = true });
        
        // Floor 2: ONLY in main section (right half)
        if (stories >= 2)
        {
            rooms.Add(new Room { Name = "Master Bedroom", Floor = 2,
                X = mainMinX, Z = 0, Width = mainW * 0.6, Depth = depth * 0.5,
                WindowCount = 2, HasDoor = true });
            rooms.Add(new Room { Name = "Bedroom 2", Floor = 2,
                X = mainMinX + mainW * 0.6, Z = 0, Width = mainW * 0.4, Depth = depth * 0.5,
                WindowCount = 1, HasDoor = true });
            rooms.Add(new Room { Name = "Bathroom", Floor = 2,
                X = mainMinX, Z = depth * 0.5, Width = mainW * 0.4, Depth = depth * 0.5,
                WindowCount = 1, HasDoor = true });
            rooms.Add(new Room { Name = "Office", Floor = 2,
                X = mainMinX + mainW * 0.4, Z = depth * 0.5, Width = mainW * 0.6, Depth = depth * 0.5,
                WindowCount = 2, HasDoor = true });
        }
        
        return rooms;
    }

    /// <summary>
    /// Calculate number of windows based on room dimensions and window-to-wall ratio
    /// </summary>
    private int CalculateWindowCount(double width, double depth, double windowToWallRatio)
    {
        // Calculate perimeter of room
        double perimeter = 2 * (width + depth);
        // Use default ceiling height for wall area calculation
        double wallArea = perimeter * ArchitecturalConstants.DefaultCeilingHeight;
        // Calculate target window area
        double targetWindowArea = wallArea * windowToWallRatio;
        // Standard window size (3ft × 4ft)
        int windowCount = (int)Math.Ceiling(targetWindowArea / ArchitecturalConstants.DefaultWindowArea);
        // Constrain to reasonable range
        return Math.Max(ArchitecturalConstants.MinWindowsPerRoom, Math.Min(ArchitecturalConstants.MaxWindowsPerRoom, windowCount));
    }
}
