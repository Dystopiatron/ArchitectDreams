namespace ArchitecturalDreamMachineBackend.Geometry;

public class HouseParameters
{
    public double LotSize { get; set; }
    public string RoofType { get; set; } = string.Empty;
    public string WindowStyle { get; set; } = string.Empty;
    public int RoomCount { get; set; }
    public Material Material { get; set; } = new();

    public Mesh GenerateMesh()
    {
        var mesh = new Mesh();
        
        // Calculate base dimensions from lot size
        float baseSize = (float)Math.Sqrt(LotSize);
        float height = baseSize * 0.6f; // Height proportional to base
        
        // Generate cube vertices (8 vertices for a simple house box)
        mesh.Vertices = new List<Vector3>
        {
            // Bottom face
            new Vector3(-baseSize/2, 0, -baseSize/2),
            new Vector3(baseSize/2, 0, -baseSize/2),
            new Vector3(baseSize/2, 0, baseSize/2),
            new Vector3(-baseSize/2, 0, baseSize/2),
            // Top face
            new Vector3(-baseSize/2, height, -baseSize/2),
            new Vector3(baseSize/2, height, -baseSize/2),
            new Vector3(baseSize/2, height, baseSize/2),
            new Vector3(-baseSize/2, height, baseSize/2)
        };
        
        // Generate indices for cube faces (36 indices = 6 faces * 2 triangles * 3 vertices)
        mesh.Indices = new List<int>
        {
            // Bottom
            0, 2, 1, 0, 3, 2,
            // Top
            4, 5, 6, 4, 6, 7,
            // Front
            0, 1, 5, 0, 5, 4,
            // Back
            2, 3, 7, 2, 7, 6,
            // Left
            3, 0, 4, 3, 4, 7,
            // Right
            1, 2, 6, 1, 6, 5
        };
        
        return mesh;
    }
}
