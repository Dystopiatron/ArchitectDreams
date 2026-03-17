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
        // Keep single-digit numbers (for "3 story" etc.) but filter other 1-char words
        var keywords = words
            .Select(w => w.ToLowerInvariant())
            .Where(w => !StopWords.Contains(w) && (w.Length > 1 || char.IsDigit(w[0])))
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

    /// <summary>
    /// Extract number of stories from keywords (e.g., "3", "story" → 3)
    /// </summary>
    public static int? ExtractStories(List<string> keywords)
    {
        // Map word forms to numbers
        var numberWords = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "one", 1 }, { "single", 1 },
            { "two", 2 }, { "double", 2 },
            { "three", 3 }, { "triple", 3 },
            { "four", 4 },
            { "five", 5 },
            { "six", 6 },
            { "seven", 7 },
            { "eight", 8 },
            { "nine", 9 },
            { "ten", 10 }
        };

        // Check if "story" or "stories" is in keywords
        bool hasStoryKeyword = keywords.Any(k => 
            k.Equals("story", StringComparison.OrdinalIgnoreCase) || 
            k.Equals("stories", StringComparison.OrdinalIgnoreCase) ||
            k.Equals("storey", StringComparison.OrdinalIgnoreCase) ||
            k.Equals("storeys", StringComparison.OrdinalIgnoreCase) ||
            k.Equals("floor", StringComparison.OrdinalIgnoreCase) ||
            k.Equals("floors", StringComparison.OrdinalIgnoreCase));

        if (!hasStoryKeyword)
            return null;

        // Look for a number (digit or word)
        foreach (var keyword in keywords)
        {
            // Try numeric
            if (int.TryParse(keyword, out int numericStories) && numericStories >= 1 && numericStories <= 10)
            {
                return numericStories;
            }

            // Try word form
            if (numberWords.TryGetValue(keyword, out int wordStories))
            {
                return wordStories;
            }
        }

        return null;
    }

    /// <summary>
    /// Extract building shape from keywords
    /// </summary>
    public static string? ExtractBuildingShape(List<string> keywords)
    {
        var shapeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "cube", "cube" },
            { "box", "cube" },
            { "rectangular", "cube" },
            { "simple", "cube" },
            { "lshape", "l-shape" },
            { "split", "split-level" },
            { "splitlevel", "split-level" },
            { "angled", "angled" },
            { "angular", "angled" },
            { "twostory", "two-story" }
        };

        foreach (var keyword in keywords)
        {
            if (shapeMap.TryGetValue(keyword, out string? shape))
            {
                return shape;
            }
        }

        return null;
    }
}
