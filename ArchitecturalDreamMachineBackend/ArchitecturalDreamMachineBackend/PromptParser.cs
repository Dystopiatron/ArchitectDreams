using System.Text.RegularExpressions;

namespace ArchitecturalDreamMachineBackend;

public static class PromptParser
{
    /// <summary>
    /// Maximum allowed prompt length
    /// </summary>
    public const int MaxPromptLength = 500;
    
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "sq", "ft", "feet", "square", "a", "an", "the", "with", "and", "or", "in", "on", "at", "to", "for", "of", "is", "are"
    };

    /// <summary>
    /// Parse a style prompt into keywords, with input sanitization
    /// </summary>
    /// <param name="prompt">User-provided style prompt</param>
    /// <returns>List of sanitized, parsed keywords</returns>
    public static List<string> Parse(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return new List<string>();

        // Step 1: Limit input length to prevent DoS
        var sanitized = prompt.Length > MaxPromptLength 
            ? prompt.Substring(0, MaxPromptLength) 
            : prompt;
        
        // Step 2: Remove potentially harmful characters (XSS, SQL injection patterns)
        // Removes: < > " ' & ; \ ` $ { } [ ] | and control characters
        sanitized = Regex.Replace(sanitized, @"[<>""'&;\\`${}\[\]|\x00-\x1F]", "");

        // Step 3: Remove punctuation (keep alphanumeric and spaces)
        var noPunctuation = Regex.Replace(sanitized, @"[^\w\s]", " ");
        
        // Step 4: Split by space or comma
        var words = noPunctuation.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
        
        // Step 5: Convert to lowercase, filter stop words, and remove duplicates
        var keywords = words
            .Select(w => w.ToLowerInvariant())
            .Where(w => !StopWords.Contains(w) && w.Length > 1)
            .Distinct()
            .ToList();
        
        return keywords;
    }
    
    /// <summary>
    /// Sanitize a prompt string without parsing into keywords
    /// Useful for storing the original prompt safely
    /// </summary>
    /// <param name="prompt">User-provided prompt</param>
    /// <returns>Sanitized prompt string</returns>
    public static string Sanitize(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return string.Empty;
            
        // Limit length
        var sanitized = prompt.Length > MaxPromptLength 
            ? prompt.Substring(0, MaxPromptLength) 
            : prompt;
        
        // Remove harmful characters but preserve readable punctuation
        sanitized = Regex.Replace(sanitized, @"[<>""'&;\\`${}\[\]|\x00-\x1F]", "");
        
        return sanitized.Trim();
    }
}
