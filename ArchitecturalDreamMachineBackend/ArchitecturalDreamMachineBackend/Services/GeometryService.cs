using ArchitecturalDreamMachineBackend.Models;

namespace ArchitecturalDreamMachineBackend.Services
{
    /// <summary>
    /// Main service for generating geometric data that can be directly rendered by Three.js
    /// Combines VertexCalculator and FaceGenerator to produce complete GeometryData objects
    /// </summary>
    public class GeometryService
    {
        /// <summary>
        /// Create a box geometry (building section, floor platform, etc.)
        /// </summary>
        /// <param name="width">Width (X dimension)</param>
        /// <param name="height">Height (Y dimension)</param>
        /// <param name="depth">Depth (Z dimension)</param>
        /// <param name="x">X position offset</param>
        /// <param name="y">Y position offset</param>
        /// <param name="z">Z position offset</param>
        /// <param name="materialType">Material type for rendering</param>
        /// <param name="color">Color name or hex</param>
        /// <returns>Complete geometry ready for Three.js</returns>
        public GeometryData CreateBox(
            double width, 
            double height, 
            double depth, 
            double x, 
            double y, 
            double z,
            string materialType = "stucco",
            string color = "white")
        {
            var vertices = Geometry.VertexCalculator.CalculateBoxVertices(width, height, depth);
            var indices = Geometry.FaceGenerator.BoxFaces();
            
            return new GeometryData
            {
                Vertices = vertices,
                Indices = indices,
                MaterialType = materialType,
                Color = color,
                Position = new Position { X = x, Y = y, Z = z }
            };
        }
        
        /// <summary>
        /// Create a traditional gabled roof geometry
        /// USES Phase 1.1 calculation: roofHeight = (roofWidth / 2) * pitchRatio
        /// </summary>
        /// <param name="width">Building width (roof spans this dimension)</param>
        /// <param name="depth">Building depth (ridge runs this direction)</param>
        /// <param name="roofPitch">Roof pitch as rise over 12 (e.g., 8.0 for 8:12)</param>
        /// <param name="overhang">Horizontal overhang beyond walls</param>
        /// <returns>Gabled roof geometry</returns>
        public GeometryData CreateGabledRoof(
            double width, 
            double depth, 
            double roofPitch, 
            double overhang)
        {
            // Calculate pitch ratio (rise over run)
            double pitchRatio = roofPitch / 12.0;
            
            // Apply overhang first, then calculate height
            // This matches Phase 1.1 fix: roofHeight = (roofWidth / 2) * pitchRatio
            double roofWidth = width + (overhang * 2);
            double roofHeight = (roofWidth / 2) * pitchRatio;
            
            var vertices = Geometry.VertexCalculator.CalculateGabledRoofVertices(
                width, depth, roofHeight, overhang);
            var indices = Geometry.FaceGenerator.GabledRoofFaces();
            
            return new GeometryData
            {
                Vertices = vertices,
                Indices = indices,
                MaterialType = "roof",
                Color = "#8b4513", // Brown roof
                Position = new Position { X = 0, Y = 0, Z = 0 } // Position set by caller
            };
        }
        
        /// <summary>
        /// Create a flat roof geometry (thin box)
        /// </summary>
        public GeometryData CreateFlatRoof(
            double width, 
            double depth, 
            double overhang,
            double thickness = 0.75)
        {
            var vertices = Geometry.VertexCalculator.CalculateFlatRoofVertices(
                width, depth, thickness, overhang);
            var indices = Geometry.FaceGenerator.FlatRoofFaces();
            
            return new GeometryData
            {
                Vertices = vertices,
                Indices = indices,
                MaterialType = "roof",
                Color = "#333333", // Dark gray
                Position = new Position { X = 0, Y = 0, Z = 0 }
            };
        }
        
        /// <summary>
        /// Create parapet wall geometries for flat roofs
        /// Returns 4 geometries (front, back, left, right)
        /// </summary>
        public List<GeometryData> CreateParapetWalls(
            double width,
            double depth,
            double overhang,
            double parapetHeight = 2.5,
            double parapetThickness = 0.5)
        {
            double roofWidth = width + (overhang * 2);
            double roofDepth = depth + (overhang * 2);
            
            var parapets = new List<GeometryData>();
            
            // Front and back walls (along width)
            var frontBackVertices = Geometry.VertexCalculator.CalculateParapetVertices(
                roofWidth + parapetThickness, parapetHeight, parapetThickness);
            var parapetIndices = Geometry.FaceGenerator.ParapetFaces();
            
            // Front wall (Z+)
            parapets.Add(new GeometryData
            {
                Vertices = frontBackVertices,
                Indices = parapetIndices,
                MaterialType = "concrete",
                Color = "#e0e0e0",
                Position = new Position { X = 0, Y = parapetHeight / 2, Z = roofDepth / 2 }
            });
            
            // Back wall (Z-)
            parapets.Add(new GeometryData
            {
                Vertices = frontBackVertices,
                Indices = parapetIndices,
                MaterialType = "concrete",
                Color = "#e0e0e0",
                Position = new Position { X = 0, Y = parapetHeight / 2, Z = -roofDepth / 2 }
            });
            
            // Left and right walls (along depth)
            var leftRightVertices = Geometry.VertexCalculator.CalculateParapetVertices(
                roofDepth, parapetHeight, parapetThickness);
            
            // Right wall (X+) - rotated 90 degrees
            parapets.Add(new GeometryData
            {
                Vertices = leftRightVertices,
                Indices = parapetIndices,
                MaterialType = "concrete",
                Color = "#e0e0e0",
                Position = new Position { X = roofWidth / 2, Y = parapetHeight / 2, Z = 0 }
            });
            
            // Left wall (X-) - rotated 90 degrees
            parapets.Add(new GeometryData
            {
                Vertices = leftRightVertices,
                Indices = parapetIndices,
                MaterialType = "concrete",
                Color = "#e0e0e0",
                Position = new Position { X = -roofWidth / 2, Y = parapetHeight / 2, Z = 0 }
            });
            
            return parapets;
        }
        
        /// <summary>
        /// Create a simple quad for windows or doors
        /// </summary>
        public GeometryData CreateQuad(
            double width,
            double height,
            double x,
            double y,
            double z,
            string materialType = "glass",
            string color = "#87ceeb")
        {
            float hw = (float)(width / 2);
            float hh = (float)(height / 2);
            
            var vertices = new float[]
            {
                -hw, -hh, 0,
                hw, -hh, 0,
                hw, hh, 0,
                -hw, hh, 0
            };
            
            var indices = Geometry.FaceGenerator.QuadFaces();
            
            return new GeometryData
            {
                Vertices = vertices,
                Indices = indices,
                MaterialType = materialType,
                Color = color,
                Position = new Position { X = x, Y = y, Z = z }
            };
        }
    }
}
