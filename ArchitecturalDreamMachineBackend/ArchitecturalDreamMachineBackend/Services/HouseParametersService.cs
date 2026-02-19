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

        // Generate room layout
        var rooms = GenerateRoomLayout(
            footprintWidth,
            footprintDepth,
            styleTemplate.RoomCount,
            stories,
            buildingShape
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
    private List<Room> GenerateRoomLayout(
        double width,
        double depth,
        int roomCount,
        int stories,
        string shape)
    {
        var rooms = new List<Room>();

        // L-shape requires rooms constrained to the two-wing footprint so that windows
        // and interior walls don't appear in the void (front-right) quadrant.
        if (shape == "l-shape")
            return GenerateLShapeRoomLayout(width, depth, roomCount, stories);

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
                WindowCount = CalculateWindowCount(width, depth * 0.6, 0.15),
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
                    WindowCount = CalculateWindowCount(width * 0.6, depth * 0.5, 0.15),
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
                    WindowCount = CalculateWindowCount(width * 0.6, depth * 0.7, 0.15),
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
                    WindowCount = CalculateWindowCount(width * 0.5, depth * 0.5, 0.15),
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
                WindowCount = 1, HasDoor = true });
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
                WindowCount = 2, HasDoor = true });
            rooms.Add(new Room { Name = "Powder Room", Floor = 1,
                X = 0, Z = backDepth, Width = cornerWidth, Depth = frontDepth,
                WindowCount = 0, HasDoor = true });
            // Floor 2: 2 bedrooms in main wing
            rooms.Add(new Room { Name = "Master Bedroom", Floor = 2,
                X = 0, Z = 0, Width = cornerWidth, Depth = backDepth,
                WindowCount = 2, HasDoor = true });
            rooms.Add(new Room { Name = "Bedroom 2", Floor = 2,
                X = cornerWidth, Z = 0, Width = cornerWidth, Depth = backDepth,
                WindowCount = 1, HasDoor = true });
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
                WindowCount = 1, HasDoor = true });
            rooms.Add(new Room { Name = "Entry", Floor = 1,
                X = 0, Z = backDepth, Width = cornerWidth, Depth = frontDepth,
                WindowCount = 1, HasDoor = true });
            // Floor 2: 2 main-wing bedrooms + 2 corner-wing rooms
            rooms.Add(new Room { Name = "Master Bedroom", Floor = 2,
                X = 0, Z = 0, Width = cornerWidth, Depth = backDepth,
                WindowCount = 2, HasDoor = true });
            rooms.Add(new Room { Name = "Bedroom 2", Floor = 2,
                X = cornerWidth, Z = 0, Width = cornerWidth, Depth = backDepth,
                WindowCount = 1, HasDoor = true });
            rooms.Add(new Room { Name = "Bedroom 3", Floor = 2,
                X = 0, Z = backDepth, Width = cornerWidth * 0.5, Depth = frontDepth,
                WindowCount = 1, HasDoor = true });
            rooms.Add(new Room { Name = "Bathroom", Floor = 2,
                X = cornerWidth * 0.5, Z = backDepth,
                Width = cornerWidth * 0.5, Depth = frontDepth,
                WindowCount = 0, HasDoor = true });
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
