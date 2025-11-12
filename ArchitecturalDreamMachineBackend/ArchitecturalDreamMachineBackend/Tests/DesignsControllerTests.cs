using Xunit;
using Microsoft.EntityFrameworkCore;
using ArchitecturalDreamMachineBackend.Data;
using ArchitecturalDreamMachineBackend.Controllers;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.AspNetCore.Mvc;

namespace ArchitecturalDreamMachineBackend.Tests;

public class DesignsControllerTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        
        // Seed data
        context.StyleTemplates.AddRange(
            new StyleTemplate
            {
                Id = 1,
                Name = "Brutalist",
                RoofType = "flat",
                WindowStyle = "small",
                RoomCount = 4,
                Color = "gray",
                Texture = "concrete"
            },
            new StyleTemplate
            {
                Id = 2,
                Name = "Victorian",
                RoofType = "gabled",
                WindowStyle = "ornate",
                RoomCount = 6,
                Color = "cream",
                Texture = "wood"
            },
            new StyleTemplate
            {
                Id = 3,
                Name = "Modern",
                RoofType = "flat",
                WindowStyle = "large",
                RoomCount = 5,
                Color = "white",
                Texture = "glass"
            }
        );
        context.SaveChanges();

        return context;
    }

    [Fact]
    public async Task Generate_ValidInput_ReturnsOkWithHouseParameters()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var logger = new Mock<ILogger<DesignsController>>();
        var controller = new DesignsController(context, logger.Object);

        var request = new GenerateRequest
        {
            LotSize = 2500,
            StylePrompt = "Modern minimalist design"
        };

        // Act
        var result = await controller.Generate(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
        
        // Verify design was saved
        Assert.Single(context.Designs);
        var savedDesign = await context.Designs.FirstAsync();
        Assert.Equal(2500, savedDesign.LotSize);
    }

    [Fact]
    public async Task Generate_InvalidLotSize_ReturnsBadRequest()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var logger = new Mock<ILogger<DesignsController>>();
        var controller = new DesignsController(context, logger.Object);

        var request = new GenerateRequest
        {
            LotSize = -100,
            StylePrompt = "Modern"
        };

        // Act
        var result = await controller.Generate(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Generate_EmptyStylePrompt_ReturnsBadRequest()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var logger = new Mock<ILogger<DesignsController>>();
        var controller = new DesignsController(context, logger.Object);

        var request = new GenerateRequest
        {
            LotSize = 2500,
            StylePrompt = ""
        };

        // Act
        var result = await controller.Generate(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetAll_ReturnsAllDesigns()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var logger = new Mock<ILogger<DesignsController>>();
        var controller = new DesignsController(context, logger.Object);

        // Add test designs
        context.Designs.AddRange(
            new Design { LotSize = 2500, StyleKeywords = "modern" },
            new Design { LotSize = 3000, StyleKeywords = "victorian" }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var designs = Assert.IsAssignableFrom<IEnumerable<Design>>(okResult.Value);
        Assert.Equal(2, designs.Count());
    }
}
