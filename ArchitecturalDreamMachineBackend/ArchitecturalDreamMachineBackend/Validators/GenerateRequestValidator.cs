using FluentValidation;
using ArchitecturalDreamMachineBackend.Controllers;

namespace ArchitecturalDreamMachineBackend.Validators;

/// <summary>
/// Validator for GenerateRequest using FluentValidation
/// Provides declarative validation rules instead of manual if-statements
/// </summary>
public class GenerateRequestValidator : AbstractValidator<GenerateRequest>
{
    public GenerateRequestValidator()
    {
        RuleFor(x => x.LotSize)
            .GreaterThan(0)
            .WithMessage("Lot size must be greater than 0")
            .LessThanOrEqualTo(100000)
            .WithMessage("Lot size cannot exceed 100,000 sq ft");
        
        RuleFor(x => x.StylePrompt)
            .NotEmpty()
            .WithMessage("Style prompt is required")
            .MaximumLength(500)
            .WithMessage("Style prompt cannot exceed 500 characters");
        
        RuleFor(x => x.BuildingShapeOverride)
            .MaximumLength(50)
            .When(x => x.BuildingShapeOverride != null)
            .WithMessage("Building shape override cannot exceed 50 characters")
            .Must(BeValidBuildingShape)
            .When(x => !string.IsNullOrEmpty(x.BuildingShapeOverride))
            .WithMessage("Building shape must be one of: cube, two-story, l-shape, split-level, angled");
        
        RuleFor(x => x.StoriesOverride)
            .InclusiveBetween(1, 10)
            .When(x => x.StoriesOverride.HasValue)
            .WithMessage("Stories must be between 1 and 10");
    }
    
    /// <summary>
    /// Validates that the building shape is one of the supported types
    /// </summary>
    private static bool BeValidBuildingShape(string? shape)
    {
        if (string.IsNullOrEmpty(shape))
            return true;
            
        var validShapes = new[] { "cube", "two-story", "l-shape", "split-level", "angled" };
        return validShapes.Contains(shape.ToLowerInvariant());
    }
}
