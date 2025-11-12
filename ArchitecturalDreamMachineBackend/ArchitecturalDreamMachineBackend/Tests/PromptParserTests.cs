using Xunit;

namespace ArchitecturalDreamMachineBackend.Tests;

public class PromptParserTests
{
    [Fact]
    public void Parse_ValidPrompt_ReturnsKeywords()
    {
        // Arrange
        var prompt = "Modern minimalist design with large windows";

        // Act
        var keywords = PromptParser.Parse(prompt);

        // Assert
        Assert.Contains("modern", keywords);
        Assert.Contains("minimalist", keywords);
        Assert.Contains("design", keywords);
        Assert.Contains("large", keywords);
        Assert.Contains("windows", keywords);
        Assert.DoesNotContain("with", keywords); // Stop word
    }

    [Fact]
    public void Parse_EmptyPrompt_ReturnsEmpty()
    {
        // Arrange
        var prompt = "";

        // Act
        var keywords = PromptParser.Parse(prompt);

        // Assert
        Assert.Empty(keywords);
    }

    [Fact]
    public void Parse_PromptWithPunctuation_RemovesPunctuation()
    {
        // Arrange
        var prompt = "Victorian, ornate, beautiful!";

        // Act
        var keywords = PromptParser.Parse(prompt);

        // Assert
        Assert.Contains("victorian", keywords);
        Assert.Contains("ornate", keywords);
        Assert.Contains("beautiful", keywords);
    }

    [Fact]
    public void Parse_DuplicateWords_ReturnsDistinct()
    {
        // Arrange
        var prompt = "modern modern design modern";

        // Act
        var keywords = PromptParser.Parse(prompt);

        // Assert
        Assert.Single(keywords, k => k == "modern");
        Assert.Contains("design", keywords);
    }
}
