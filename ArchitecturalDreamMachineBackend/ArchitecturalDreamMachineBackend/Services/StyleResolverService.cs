using Microsoft.EntityFrameworkCore;
using ArchitecturalDreamMachineBackend.Data;

namespace ArchitecturalDreamMachineBackend.Services;

/// <summary>
/// Interface for resolving style templates from keywords
/// </summary>
public interface IStyleResolverService
{
    /// <summary>
    /// Resolve a StyleTemplate from an array of keywords (e.g., from PromptParser)
    /// First matching keyword wins; falls back to Modern if no match
    /// </summary>
    /// <param name="keywords">Keywords parsed from user prompt</param>
    /// <returns>Matching StyleTemplate, or Modern as fallback, or null if no templates exist</returns>
    Task<StyleTemplate?> ResolveFromKeywordsAsync(IEnumerable<string> keywords);
    
    /// <summary>
    /// Resolve a StyleTemplate from a comma-separated keywords string (e.g., from Design.StyleKeywords)
    /// </summary>
    /// <param name="styleKeywords">Comma-separated keywords string</param>
    /// <returns>Matching StyleTemplate, or Modern as fallback, or null if no templates exist</returns>
    Task<StyleTemplate?> ResolveFromStoredKeywordsAsync(string styleKeywords);
}

/// <summary>
/// Service for resolving StyleTemplate from parsed or stored keywords
/// Consolidates duplicated style matching logic from controller
/// </summary>
public class StyleResolverService : IStyleResolverService
{
    private readonly AppDbContext _context;
    private readonly ILogger<StyleResolverService> _logger;
    
    /// <summary>
    /// Default style name when no match found
    /// </summary>
    private const string DefaultStyleName = "Modern";

    public StyleResolverService(AppDbContext context, ILogger<StyleResolverService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<StyleTemplate?> ResolveFromKeywordsAsync(IEnumerable<string> keywords)
    {
        StyleTemplate? styleTemplate = null;
        
        foreach (var keyword in keywords)
        {
            styleTemplate = await _context.StyleTemplates
                .FirstOrDefaultAsync(st => st.Name.ToLower().Contains(keyword.ToLower()));
            
            if (styleTemplate != null)
            {
                _logger.LogDebug("Matched style '{Style}' from keyword '{Keyword}'", 
                    styleTemplate.Name, keyword);
                break;
            }
        }

        // Fallback to default style if no match
        if (styleTemplate == null)
        {
            styleTemplate = await _context.StyleTemplates
                .FirstOrDefaultAsync(st => st.Name == DefaultStyleName);
            
            if (styleTemplate != null)
            {
                _logger.LogDebug("No keyword match, using default style '{Style}'", DefaultStyleName);
            }
        }

        return styleTemplate;
    }

    /// <inheritdoc/>
    public async Task<StyleTemplate?> ResolveFromStoredKeywordsAsync(string styleKeywords)
    {
        if (string.IsNullOrWhiteSpace(styleKeywords))
        {
            return await _context.StyleTemplates
                .FirstOrDefaultAsync(st => st.Name == DefaultStyleName);
        }
        
        var keywords = styleKeywords
            .Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
            .Select(k => k.Trim().ToLower());
        
        return await ResolveFromKeywordsAsync(keywords);
    }
}
