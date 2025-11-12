using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ArchitecturalDreamMachineBackend.Data;
using ArchitecturalDreamMachineBackend.Geometry;
using ArchitecturalDreamMachineBackend.Export;

namespace ArchitecturalDreamMachineBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DesignsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<DesignsController> _logger;

    public DesignsController(AppDbContext context, ILogger<DesignsController> logger)
    {
        _context = context;
        _logger = logger;
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
                }
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

            // Return HouseParameters with mesh
            return Ok(new
            {
                houseParameters,
                mesh = houseParameters.GenerateMesh(),
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
                }
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
}

public class GenerateRequest
{
    public double LotSize { get; set; }
    public string StylePrompt { get; set; } = string.Empty;
}
