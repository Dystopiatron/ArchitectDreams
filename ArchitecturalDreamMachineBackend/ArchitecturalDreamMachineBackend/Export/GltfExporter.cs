using System.Numerics;
using ArchitecturalDreamMachineBackend.Geometry;
using ArchitecturalDreamMachineBackend.Models;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;
using VERTEX = SharpGLTF.Geometry.VertexTypes.VertexPosition;

namespace ArchitecturalDreamMachineBackend.Export;

public class GltfExporter : IGltfExporter
{
    private readonly ILogger<GltfExporter> _logger;

    public GltfExporter(ILogger<GltfExporter> logger)
    {
        _logger = logger;
    }

    public byte[] ExportToGlb(HouseParameters parameters, BuildingGeometry geometry, string styleName)
    {
        var scene = new SceneBuilder($"ArchitecturalDreamMachine - {styleName}");

        var materials = CreateMaterials(styleName);

        for (int i = 0; i < geometry.Sections.Count; i++)
        {
            var section = geometry.Sections[i];
            var mesh = ConvertGeometryData(section, $"Section_{i}", materials);
            if (mesh != null)
                AddMeshToScene(scene, mesh, section.Position, section.Rotation);
        }

        for (int i = 0; i < geometry.Roofs.Count; i++)
        {
            var roof = geometry.Roofs[i];
            var mesh = ConvertGeometryData(roof.Geometry, $"Roof_{i}", materials);
            if (mesh != null)
                AddMeshToScene(scene, mesh, roof.Geometry.Position, roof.Geometry.Rotation);
        }

        for (int i = 0; i < geometry.Windows.Count; i++)
        {
            var window = geometry.Windows[i];
            var mesh = ConvertGeometryData(window, $"Window_{i}", materials);
            if (mesh != null)
                AddMeshToScene(scene, mesh, window.Position, window.Rotation);
        }

        for (int i = 0; i < geometry.InteriorWalls.Count; i++)
        {
            var wall = geometry.InteriorWalls[i];
            var mesh = ConvertGeometryData(wall, $"InteriorWall_{i}", materials);
            if (mesh != null)
                AddMeshToScene(scene, mesh, wall.Position, wall.Rotation);
        }

        if (geometry.Foundation != null)
        {
            var mesh = ConvertGeometryData(geometry.Foundation, "Foundation", materials);
            if (mesh != null)
                AddMeshToScene(scene, mesh, geometry.Foundation.Position, geometry.Foundation.Rotation);
        }

        var model = scene.ToGltf2();

        using var ms = new MemoryStream();
        model.WriteGLB(ms);

        _logger.LogInformation(
            "Exported design to GLB: {Sections} sections, {Roofs} roofs, {Windows} windows, {Walls} interior walls",
            geometry.Sections.Count, geometry.Roofs.Count, geometry.Windows.Count, geometry.InteriorWalls.Count);

        return ms.ToArray();
    }

    private MeshBuilder<VERTEX>? ConvertGeometryData(
        GeometryData data, string name, Dictionary<string, MaterialBuilder> materials)
    {
        if (data.Vertices.Length < 9 || data.Indices.Length < 3)
            return null;

        if (data.Vertices.Length % 3 != 0)
        {
            _logger.LogWarning("Skipping {Name}: vertex count not divisible by 3", name);
            return null;
        }

        var material = ResolveMaterial(data.MaterialType, data.Color, materials);
        var mesh = new MeshBuilder<VERTEX>(name);
        var primitive = mesh.UsePrimitive(material);

        int vertexCount = data.Vertices.Length / 3;

        for (int i = 0; i < data.Indices.Length - 2; i += 3)
        {
            int i0 = data.Indices[i];
            int i1 = data.Indices[i + 1];
            int i2 = data.Indices[i + 2];

            if (i0 < 0 || i0 >= vertexCount ||
                i1 < 0 || i1 >= vertexCount ||
                i2 < 0 || i2 >= vertexCount)
            {
                continue;
            }

            var v0 = new VERTEX(data.Vertices[i0 * 3], data.Vertices[i0 * 3 + 1], data.Vertices[i0 * 3 + 2]);
            var v1 = new VERTEX(data.Vertices[i1 * 3], data.Vertices[i1 * 3 + 1], data.Vertices[i1 * 3 + 2]);
            var v2 = new VERTEX(data.Vertices[i2 * 3], data.Vertices[i2 * 3 + 1], data.Vertices[i2 * 3 + 2]);

            primitive.AddTriangle(v0, v1, v2);
        }

        return mesh;
    }

    private static void AddMeshToScene(
        SceneBuilder scene, MeshBuilder<VERTEX> mesh,
        Models.Position? position, Models.Rotation? rotation)
    {
        var transform = Matrix4x4.Identity;

        if (rotation != null)
        {
            transform *= Matrix4x4.CreateRotationX((float)rotation.X)
                       * Matrix4x4.CreateRotationY((float)rotation.Y)
                       * Matrix4x4.CreateRotationZ((float)rotation.Z);
        }

        if (position != null)
        {
            transform *= Matrix4x4.CreateTranslation(
                (float)position.X, (float)position.Y, (float)position.Z);
        }

        scene.AddRigidMesh(mesh, transform);
    }

    private static MaterialBuilder ResolveMaterial(
        string materialType, string color, Dictionary<string, MaterialBuilder> materials)
    {
        string key = $"{materialType}_{color}".ToLowerInvariant();
        if (materials.TryGetValue(key, out var cached))
            return cached;

        if (materials.TryGetValue(materialType.ToLowerInvariant(), out cached))
            return cached;

        var parsed = ParseColor(color);
        var mat = CreatePbrMaterial(key, parsed, 0f, 0.7f);

        materials[key] = mat;
        return mat;
    }

    private static MaterialBuilder CreatePbrMaterial(string name, Vector4 baseColor, float metallic, float roughness)
    {
        return new MaterialBuilder(name)
            .WithMetallicRoughnessShader()
            .WithChannelParam(KnownChannel.BaseColor, KnownProperty.RGBA, baseColor)
            .WithChannelParam(KnownChannel.MetallicRoughness, KnownProperty.MetallicFactor, metallic)
            .WithChannelParam(KnownChannel.MetallicRoughness, KnownProperty.RoughnessFactor, roughness);
    }

    private static Dictionary<string, MaterialBuilder> CreateMaterials(string styleName)
    {
        var materials = new Dictionary<string, MaterialBuilder>(StringComparer.OrdinalIgnoreCase);

        var (wallColor, roofColor) = styleName.ToLowerInvariant() switch
        {
            "victorian" => (new Vector4(0.96f, 0.93f, 0.84f, 1f), new Vector4(0.40f, 0.26f, 0.13f, 1f)),
            "brutalist" => (new Vector4(0.60f, 0.60f, 0.60f, 1f), new Vector4(0.50f, 0.50f, 0.50f, 1f)),
            _ =>           (new Vector4(0.95f, 0.95f, 0.95f, 1f), new Vector4(0.35f, 0.35f, 0.40f, 1f)),
        };

        materials["stucco"] = CreatePbrMaterial("Stucco", wallColor, 0f, 0.9f);
        materials["concrete"] = CreatePbrMaterial("Concrete", new Vector4(0.60f, 0.60f, 0.60f, 1f), 0f, 0.95f);
        materials["wood siding"] = CreatePbrMaterial("WoodSiding", new Vector4(0.55f, 0.35f, 0.20f, 1f), 0f, 0.85f);
        materials["brick"] = CreatePbrMaterial("Brick", new Vector4(0.65f, 0.30f, 0.20f, 1f), 0f, 0.85f);

        // Glass (windows) - transparent
        materials["glass"] = new MaterialBuilder("Glass")
            .WithMetallicRoughnessShader()
            .WithChannelParam(KnownChannel.BaseColor, KnownProperty.RGBA, new Vector4(0.6f, 0.8f, 0.9f, 0.3f))
            .WithChannelParam(KnownChannel.MetallicRoughness, KnownProperty.MetallicFactor, 0.1f)
            .WithChannelParam(KnownChannel.MetallicRoughness, KnownProperty.RoughnessFactor, 0.1f)
            .WithAlpha(SharpGLTF.Materials.AlphaMode.BLEND);

        materials["roof"] = CreatePbrMaterial("Roof", roofColor, 0f, 0.8f);
        materials["drywall"] = CreatePbrMaterial("Drywall", new Vector4(0.92f, 0.90f, 0.87f, 1f), 0f, 0.95f);
        materials["foundation"] = CreatePbrMaterial("Foundation", new Vector4(0.35f, 0.45f, 0.30f, 1f), 0f, 0.9f);

        return materials;
    }

    private static Vector4 ParseColor(string color)
    {
        return color.ToLowerInvariant() switch
        {
            "white" => new Vector4(0.95f, 0.95f, 0.95f, 1f),
            "cream" => new Vector4(0.96f, 0.93f, 0.84f, 1f),
            "gray" or "grey" => new Vector4(0.60f, 0.60f, 0.60f, 1f),
            "brown" => new Vector4(0.55f, 0.35f, 0.20f, 1f),
            "red" => new Vector4(0.65f, 0.30f, 0.20f, 1f),
            "blue" => new Vector4(0.40f, 0.55f, 0.75f, 1f),
            "green" => new Vector4(0.35f, 0.55f, 0.35f, 1f),
            "black" => new Vector4(0.15f, 0.15f, 0.15f, 1f),
            _ when color.StartsWith('#') && color.Length == 7 => ParseHexColor(color),
            _ => new Vector4(0.85f, 0.85f, 0.85f, 1f),
        };
    }

    private static Vector4 ParseHexColor(string hex)
    {
        if (int.TryParse(hex.AsSpan(1, 2), System.Globalization.NumberStyles.HexNumber, null, out int r) &&
            int.TryParse(hex.AsSpan(3, 2), System.Globalization.NumberStyles.HexNumber, null, out int g) &&
            int.TryParse(hex.AsSpan(5, 2), System.Globalization.NumberStyles.HexNumber, null, out int b))
        {
            return new Vector4(r / 255f, g / 255f, b / 255f, 1f);
        }
        return new Vector4(0.85f, 0.85f, 0.85f, 1f);
    }
}
