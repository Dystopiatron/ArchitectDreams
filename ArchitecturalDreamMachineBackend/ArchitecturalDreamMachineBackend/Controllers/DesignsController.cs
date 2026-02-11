using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using ArchitecturalDreamMachineBackend.Data;
using ArchitecturalDreamMachineBackend.Geometry;
using ArchitecturalDreamMachineBackend.Export;
using ArchitecturalDreamMachineBackend.Services;
using ArchitecturalDreamMachineBackend.Models;

namespace ArchitecturalDreamMachineBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DesignsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<DesignsController> _logger;
    private readonly IDesignOrchestrationService _orchestrationService;
    private readonly IHouseParametersService _houseParametersService;
    private readonly IIfcExporter _ifcExporter;

    public DesignsController(
        AppDbContext context, 
        ILogger<DesignsController> logger,
        IDesignOrchestrationService orchestrationService,
        IHouseParametersService houseParametersService,
        IIfcExporter ifcExporter)
    {
        _context = context;
        _logger = logger;
        _orchestrationService = orchestrationService;
        _houseParametersService = houseParametersService;
        _ifcExporter = ifcExporter;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<HouseParameters>> Generate([FromBody] GenerateRequest request)
    {
        // Validation is handled automatically by FluentValidation middleware
        // ModelState will contain validation errors if any
        
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
                return StatusCode(500, new ErrorResponse("No style templates available"));
            }

            // Calculate HouseParameters using service
            var houseParameters = _houseParametersService.CalculateParameters(
                request.LotSize,
                styleTemplate,
                request.BuildingShapeOverride,
                request.StoriesOverride);

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
            return Ok(new GenerateResponse
            {
                HouseParameters = houseParameters,
                Mesh = houseParameters.GenerateMesh(),
                Geometry = geometry,
                DesignId = design.Id,
                StyleName = styleTemplate.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating design");
            return StatusCode(500, new ErrorResponse("Internal server error"));
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
            return StatusCode(500, new ErrorResponse("Internal server error"));
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
                return NotFound(new ErrorResponse("Design not found"));
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
                return StatusCode(500, new ErrorResponse("No style templates available"));
            }

            // Calculate HouseParameters using service (same logic as Generate)
            var houseParameters = _houseParametersService.CalculateParameters(
                design.LotSize,
                styleTemplate);

            // Export to OBJ format
            var objContent = ObjExporter.ExportToObj(houseParameters);

            // Return as downloadable file
            var safeName = Regex.Replace(styleTemplate.Name.ToLower(), @"[^a-zA-Z0-9_-]", "");
            var fileName = $"house_design_{id}_{safeName}.obj";
            return File(System.Text.Encoding.UTF8.GetBytes(objContent), "text/plain", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting design {DesignId}", id);
            return StatusCode(500, new ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Export a design to IFC format for BIM applications (Revit, ArchiCAD, etc.)
    /// </summary>
    [HttpGet("{id}/export/ifc")]
    public async Task<IActionResult> ExportToIfc(int id)
    {
        try
        {
            // Get the design
            var design = await _context.Designs.FindAsync(id);
            if (design == null)
            {
                return NotFound(new ErrorResponse("Design not found"));
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
                return StatusCode(500, new ErrorResponse("No style templates available"));
            }

            // Calculate HouseParameters
            var houseParameters = _houseParametersService.CalculateParameters(
                design.LotSize,
                styleTemplate);

            // Generate complete geometry
            var geometry = _orchestrationService.GenerateCompleteGeometry(houseParameters);

            // Export to IFC format
            var ifcBytes = _ifcExporter.ExportToIfc(houseParameters, geometry, styleTemplate.Name);

            // Return as downloadable file
            var safeName = Regex.Replace(styleTemplate.Name.ToLower(), @"[^a-zA-Z0-9_-]", "");
            var fileName = $"house_design_{id}_{safeName}.ifc";
            return File(ifcBytes, "application/x-step", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting design {DesignId} to IFC", id);
            return StatusCode(500, new ErrorResponse("Internal server error"));
        }
    }
}

public class GenerateRequest
{
    public double LotSize { get; set; }
    
    [MaxLength(500)]
    public string StylePrompt { get; set; } = string.Empty;
    
    // Optional overrides
    [MaxLength(50)]
    public string? BuildingShapeOverride { get; set; }
    
    [Range(1, 10)]
    public int? StoriesOverride { get; set; }
}
