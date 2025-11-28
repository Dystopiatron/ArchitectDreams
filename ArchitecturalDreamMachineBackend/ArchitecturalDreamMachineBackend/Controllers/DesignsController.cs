using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ArchitecturalDreamMachineBackend.Data;
using ArchitecturalDreamMachineBackend.Geometry;
using ArchitecturalDreamMachineBackend.Export;
using ArchitecturalDreamMachineBackend.Services;

namespace ArchitecturalDreamMachineBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DesignsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<DesignsController> _logger;
    private readonly DesignOrchestrationService _orchestrationService;

    public DesignsController(
        AppDbContext context, 
        ILogger<DesignsController> logger,
        DesignOrchestrationService orchestrationService)
    {
        _context = context;
        _logger = logger;
        _orchestrationService = orchestrationService;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<HouseParameters>> Generate([FromBody] GenerateRequest request)
    {
        // Validate input
        if (request.LotSize <= 0)
        {
            return BadRequest(new { error = "Lot size must be greater than 0" });
        }

        if (string.IsNullOrWhiteSpace(request.StylePrompt))
        {
            return BadRequest(new { error = "Style prompt is required" });
        }

        try
        {
            // Parse style prompt to keywords
            var keywords = PromptParser.Parse(request.StylePrompt);
            _logger.LogInformation("Parsed keywords: {Keywords}", string.Join(", ", keywords));

            // Query StyleTemplate based on keywords (simple matching)
            StyleTemplate? styleTemplate = null;
            
            foreach (var keyword in keywords)
            {
                styleTemplate = await _context.StyleTemplates
                    .FirstOrDefaultAsync(st => st.Name.ToLower().Contains(keyword));
                
                if (styleTemplate != null)
                    break;
            }

            // Fallback to Modern if no match
            if (styleTemplate == null)
            {
                styleTemplate = await _context.StyleTemplates
                    .FirstOrDefaultAsync(st => st.Name == "Modern");
            }

            if (styleTemplate == null)
            {
                return StatusCode(500, new { error = "No style templates available" });
            }

            // Calculate architectural dimensions
            var desiredBuildingSqFt = request.LotSize;
            // Use override if provided, otherwise use style template default
            var stories = request.StoriesOverride ?? styleTemplate.TypicalStories;
            var footprintSqFt = desiredBuildingSqFt / stories;
            
            // Rectangular footprint (1.5:1 aspect ratio)
            var footprintWidth = Math.Sqrt(footprintSqFt / 1.5);
            var footprintDepth = footprintSqFt / footprintWidth;
            
            // Generate room layout
            var rooms = GenerateRoomLayout(
                footprintWidth,
                footprintDepth,
                styleTemplate.RoomCount,
                stories,
                styleTemplate.BuildingShape
            );

            // Create HouseParameters
            var houseParameters = new HouseParameters
            {
                LotSize = request.LotSize,
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
                // Use override if provided, otherwise use style template default
                BuildingShape = request.BuildingShapeOverride ?? styleTemplate.BuildingShape,
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

            // Save Design to database
            var design = new Design
            {
                LotSize = request.LotSize,
                StyleKeywords = string.Join(", ", keywords)
            };

            _context.Designs.Add(design);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created design with ID {DesignId} using style {StyleName}", 
                design.Id, styleTemplate.Name);

            // NEW: Generate complete geometry on backend
            var geometry = _orchestrationService.GenerateCompleteGeometry(houseParameters);

            // Return HouseParameters with mesh AND complete geometry
            return Ok(new
            {
                houseParameters,
                mesh = houseParameters.GenerateMesh(),
                geometry, // NEW: Complete geometry ready for Three.js
                designId = design.Id,
                styleName = styleTemplate.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating design");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Design>>> GetAll()
    {
        try
        {
            var designs = await _context.Designs
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
            
            return Ok(designs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving designs");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("{id}/export")]
    public async Task<IActionResult> ExportToObj(int id)
    {
        try
        {
            // Get the design
            var design = await _context.Designs.FindAsync(id);
            if (design == null)
            {
                return NotFound(new { error = "Design not found" });
            }

            // Parse keywords and find style template
            var keywords = design.StyleKeywords.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
            StyleTemplate? styleTemplate = null;
            
            foreach (var keyword in keywords)
            {
                styleTemplate = await _context.StyleTemplates
                    .FirstOrDefaultAsync(st => st.Name.ToLower().Contains(keyword.ToLower()));
                
                if (styleTemplate != null)
                    break;
            }

            // Fallback to Modern if no match
            if (styleTemplate == null)
            {
                styleTemplate = await _context.StyleTemplates
                    .FirstOrDefaultAsync(st => st.Name == "Modern");
            }

            if (styleTemplate == null)
            {
                return StatusCode(500, new { error = "No style templates available" });
            }

            // Calculate architectural dimensions for export
            var desiredBuildingSqFt = design.LotSize;
            var stories = styleTemplate.TypicalStories;
            var footprintSqFt = desiredBuildingSqFt / stories;
            var footprintWidth = Math.Sqrt(footprintSqFt / 1.5);
            var footprintDepth = footprintSqFt / footprintWidth;
            
            // Generate room layout for export
            var rooms = GenerateRoomLayout(
                footprintWidth,
                footprintDepth,
                styleTemplate.RoomCount,
                stories,
                styleTemplate.BuildingShape
            );

            // Recreate HouseParameters
            var houseParameters = new HouseParameters
            {
                LotSize = design.LotSize,
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
                BuildingShape = styleTemplate.BuildingShape,
                WindowToWallRatio = styleTemplate.WindowToWallRatio,
                FoundationType = styleTemplate.FoundationType,
                ExteriorMaterial = styleTemplate.ExteriorMaterial,
                RoofPitch = styleTemplate.RoofPitch,
                HasParapet = styleTemplate.HasParapet,
                HasEaves = styleTemplate.HasEaves,
                EavesOverhang = styleTemplate.EavesOverhang,
                FootprintWidth = footprintWidth,
                FootprintDepth = footprintDepth,
                Rooms = rooms
            };

            // Export to OBJ format
            var objContent = ObjExporter.ExportToObj(houseParameters);

            // Return as downloadable file
            var fileName = $"house_design_{id}_{styleTemplate.Name.ToLower()}.obj";
            return File(System.Text.Encoding.UTF8.GetBytes(objContent), "text/plain", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting design {DesignId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private List<Room> GenerateRoomLayout(
        double width,
        double depth,
        int roomCount,
        int stories,
        string shape)
    {
        var rooms = new List<Room>();
        
        // Generate rooms for full footprint dimensions
        // Frontend will handle filtering walls based on actual building shape

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

    private int CalculateWindowCount(double width, double depth, double windowToWallRatio)
    {
        // Calculate perimeter of room
        double perimeter = 2 * (width + depth);
        // Assume 9ft ceiling height
        double wallArea = perimeter * 9.0;
        // Calculate target window area
        double targetWindowArea = wallArea * windowToWallRatio;
        // Assume 3ft × 4ft windows (12 sq ft each)
        int windowCount = (int)Math.Ceiling(targetWindowArea / 12.0);
        // Minimum 1, maximum 5 windows per room
        return Math.Max(1, Math.Min(5, windowCount));
    }
}

public class GenerateRequest
{
    public double LotSize { get; set; }
    public string StylePrompt { get; set; } = string.Empty;
    
    // Optional overrides
    public string? BuildingShapeOverride { get; set; }
    public int? StoriesOverride { get; set; }
}
