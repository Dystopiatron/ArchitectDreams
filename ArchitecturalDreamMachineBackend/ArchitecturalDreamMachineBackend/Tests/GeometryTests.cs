using Xunit;
using ArchitecturalDreamMachineBackend.Geometry;

namespace ArchitecturalDreamMachineBackend.Tests;

public class GeometryTests
{
    [Fact]
    public void HouseParameters_GenerateMesh_CreatesValidMesh()
    {
        // Arrange
        var houseParams = new HouseParameters
        {
            LotSize = 2500,
            RoofType = "flat",
            WindowStyle = "large",
            RoomCount = 5,
            Material = new Material { Color = "white", Texture = "glass" }
        };

        // Act
        var mesh = houseParams.GenerateMesh();

        // Assert
        Assert.NotNull(mesh);
        Assert.Equal(8, mesh.Vertices.Count); // 8 vertices for a cube
        Assert.Equal(36, mesh.Indices.Count); // 36 indices (6 faces * 2 triangles * 3 vertices)
    }

    [Fact]
    public void Mesh_VerticesHaveCorrectScale()
    {
        // Arrange
        var houseParams = new HouseParameters
        {
            LotSize = 2500
        };

        // Act
        var mesh = houseParams.GenerateMesh();

        // Assert
        var expectedBaseSize = (float)Math.Sqrt(2500); // 50
        var maxX = mesh.Vertices.Max(v => Math.Abs(v.X));
        Assert.Equal(expectedBaseSize / 2, maxX, 0.01);
    }
}

/// <summary>
/// Placeholder for stretch goals
/// </summary>
public class StretchGoalTests
{
    [Fact(Skip = "Stretch goal: Hugging Face NLP integration")]
    public void HuggingFace_AdvancedPromptParsing_NotImplemented()
    {
        // TODO: Integrate Hugging Face API for advanced NLP-based style extraction
        // This would improve prompt understanding beyond simple keyword matching
        Assert.True(true);
    }

    [Fact(Skip = "Stretch goal: OBJ export")]
    public void Mesh_ExportToOBJ_NotImplemented()
    {
        // TODO: Add functionality to export mesh as .obj file for use in 3D modeling software
        Assert.True(true);
    }

    [Fact(Skip = "Stretch goal: Advanced 3D features")]
    public void HouseParameters_GenerateRoofGeometry_NotImplemented()
    {
        // TODO: Generate specific roof geometry based on RoofType (gabled, flat, hip, etc.)
        Assert.True(true);
    }

    [Fact(Skip = "Stretch goal: Material textures")]
    public void Material_ApplyTextures_NotImplemented()
    {
        // TODO: Support actual texture mapping (concrete, wood, glass patterns)
        Assert.True(true);
    }
}
