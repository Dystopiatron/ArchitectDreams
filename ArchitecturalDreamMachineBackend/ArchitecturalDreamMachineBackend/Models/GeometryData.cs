namespace ArchitecturalDreamMachineBackend.Models
{
    /// <summary>
    /// Data transfer object for geometry that can be directly rendered by Three.js
    /// Contains vertices, face indices, and material information
    /// </summary>
    public class GeometryData
    {
        /// <summary>
        /// Flat array of vertex positions [x,y,z, x,y,z, ...]
        /// </summary>
        public float[] Vertices { get; set; } = Array.Empty<float>();
        
        /// <summary>
        /// Face indices - each group of 3 forms a triangle
        /// </summary>
        public int[] Indices { get; set; } = Array.Empty<int>();
        
        /// <summary>
        /// Material type (e.g., "concrete", "wood siding", "stucco")
        /// </summary>
        public string MaterialType { get; set; } = "stucco";
        
        /// <summary>
        /// Color name or hex value
        /// </summary>
        public string Color { get; set; } = "white";
        
        /// <summary>
        /// Position offset for this geometry
        /// </summary>
        public Position? Position { get; set; }
    }
    
    /// <summary>
    /// 3D position coordinates
    /// </summary>
    public class Position
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }
    
    /// <summary>
    /// Complete building geometry ready for frontend rendering
    /// </summary>
    public class BuildingGeometry
    {
        /// <summary>
        /// Building section geometries (walls, floors)
        /// </summary>
        public List<GeometryData> Sections { get; set; } = new();
        
        /// <summary>
        /// Roof geometries
        /// </summary>
        public List<RoofGeometry> Roofs { get; set; } = new();
        
        /// <summary>
        /// Window geometries
        /// </summary>
        public List<GeometryData> Windows { get; set; } = new();
        
        /// <summary>
        /// Interior wall geometries
        /// </summary>
        public List<GeometryData> InteriorWalls { get; set; } = new();
        
        /// <summary>
        /// Foundation/ground floor geometry
        /// </summary>
        public GeometryData? Foundation { get; set; }
        
        /// <summary>
        /// Total height of building including roof
        /// </summary>
        public double TotalHeight { get; set; }
        
        /// <summary>
        /// Maximum dimension (for camera positioning)
        /// </summary>
        public double MaxDimension { get; set; }
    }
    
    /// <summary>
    /// Roof-specific geometry data with additional metadata
    /// </summary>
    public class RoofGeometry
    {
        /// <summary>
        /// The geometry data for the roof
        /// </summary>
        public GeometryData Geometry { get; set; } = new();
        
        /// <summary>
        /// Height of the roof (peak above base)
        /// </summary>
        public double Height { get; set; }
        
        /// <summary>
        /// Type of roof (gabled, flat, hip, etc.)
        /// </summary>
        public string RoofType { get; set; } = "flat";
        
        /// <summary>
        /// Roof pitch (rise over run, e.g., 8.0 for 8:12)
        /// </summary>
        public double Pitch { get; set; }
    }
}
