using System.Text;
using ArchitecturalDreamMachineBackend.Geometry;

namespace ArchitecturalDreamMachineBackend.Export;

public static class ObjExporter
{
    public static string ExportToObj(HouseParameters houseParams)
    {
        var obj = new StringBuilder();
        
        // Header
        obj.AppendLine("# Architectural Dream Machine - House Export");
        obj.AppendLine($"# Lot Size: {houseParams.LotSize} sq ft");
        obj.AppendLine($"# Style: {houseParams.RoofType} roof, {houseParams.WindowStyle} windows");
        obj.AppendLine($"# Material: {houseParams.Material.Color} {houseParams.Material.Texture}");
        obj.AppendLine($"# Rooms: {houseParams.RoomCount}");
        obj.AppendLine();

        // Generate mesh
        var mesh = houseParams.GenerateMesh();
        
        // Calculate house dimensions
        var baseSize = (float)Math.Sqrt(houseParams.LotSize);
        var height = baseSize * 0.6f;

        // Write vertices
        obj.AppendLine("# Vertices");
        foreach (var vertex in mesh.Vertices)
        {
            obj.AppendLine($"v {vertex.X:F3} {vertex.Y:F3} {vertex.Z:F3}");
        }
        obj.AppendLine();

        // Write texture coordinates (optional, for future use)
        obj.AppendLine("# Texture coordinates");
        obj.AppendLine("vt 0.0 0.0");
        obj.AppendLine("vt 1.0 0.0");
        obj.AppendLine("vt 1.0 1.0");
        obj.AppendLine("vt 0.0 1.0");
        obj.AppendLine();

        // Write normals
        obj.AppendLine("# Normals");
        obj.AppendLine("vn 0.0 -1.0 0.0");  // Bottom
        obj.AppendLine("vn 0.0 1.0 0.0");   // Top
        obj.AppendLine("vn 0.0 0.0 1.0");   // Front
        obj.AppendLine("vn 0.0 0.0 -1.0");  // Back
        obj.AppendLine("vn -1.0 0.0 0.0");  // Left
        obj.AppendLine("vn 1.0 0.0 0.0");   // Right
        obj.AppendLine();

        // Write faces (groups of 3 vertices make triangles)
        obj.AppendLine("# Faces");
        obj.AppendLine("# House body");
        
        for (int i = 0; i < mesh.Indices.Count; i += 3)
        {
            // OBJ uses 1-based indexing
            var v1 = mesh.Indices[i] + 1;
            var v2 = mesh.Indices[i + 1] + 1;
            var v3 = mesh.Indices[i + 2] + 1;
            
            // Determine normal based on face
            var normalIdx = (i / 6) + 1; // Each face has 2 triangles (6 indices)
            if (normalIdx > 6) normalIdx = 6;
            
            obj.AppendLine($"f {v1}//{normalIdx} {v2}//{normalIdx} {v3}//{normalIdx}");
        }
        obj.AppendLine();

        // Add roof geometry
        obj.AppendLine("# Roof");
        obj.AppendLine($"# Roof type: {houseParams.RoofType}");
        
        if (houseParams.RoofType.ToLower() == "gabled")
        {
            // Add peaked roof vertices
            var peakHeight = height + baseSize * 0.3f;
            var roofBase = height;
            
            obj.AppendLine($"v 0.0 {peakHeight:F3} 0.0");  // Peak
            obj.AppendLine($"v {-baseSize/2:F3} {roofBase:F3} {-baseSize/2:F3}");
            obj.AppendLine($"v {baseSize/2:F3} {roofBase:F3} {-baseSize/2:F3}");
            obj.AppendLine($"v {baseSize/2:F3} {roofBase:F3} {baseSize/2:F3}");
            obj.AppendLine($"v {-baseSize/2:F3} {roofBase:F3} {baseSize/2:F3}");
            
            // Roof faces
            var startIdx = mesh.Vertices.Count + 1;
            obj.AppendLine($"f {startIdx}//2 {startIdx+1}//2 {startIdx+2}//2");  // Front slope
            obj.AppendLine($"f {startIdx}//2 {startIdx+3}//2 {startIdx+4}//2");  // Back slope
        }
        else
        {
            // Flat roof (already part of box geometry, just add comment)
            obj.AppendLine("# Flat roof is part of the main box geometry");
        }
        obj.AppendLine();

        // Add metadata
        obj.AppendLine("# Dimensions");
        obj.AppendLine($"# Base: {baseSize:F2} x {baseSize:F2} ft");
        obj.AppendLine($"# Height: {height:F2} ft");
        obj.AppendLine($"# Total volume: {(baseSize * baseSize * height):F2} cubic ft");
        
        return obj.ToString();
    }

    public static string GetFileName(int designId)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        return $"house_design_{designId}_{timestamp}.obj";
    }
}
