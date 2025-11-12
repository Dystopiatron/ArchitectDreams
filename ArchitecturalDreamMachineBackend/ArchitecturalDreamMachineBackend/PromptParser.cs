using System.Text.RegularExpressions;

namespace ArchitecturalDreamMachineBackend;

public static class PromptParser
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "sq", "ft", "feet", "square", "a", "an", "the", "with", "and", "or", "in", "on", "at", "to", "for", "of", "is", "are"
    };

    public static List<string> Parse(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return new List<string>();

        // Remove punctuation using regex
        var noPunctuation = Regex.Replace(prompt, @"[^\w\s]", " ");
        
        // Split by space or comma
        var words = noPunctuation.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
        
        // Convert to lowercase, filter stop words, and remove duplicates
        var keywords = words
            .Select(w => w.ToLowerInvariant())
            .Where(w => !StopWords.Contains(w) && w.Length > 1)
            .Distinct()
            .ToList();
        
        return keywords;
    }
}
