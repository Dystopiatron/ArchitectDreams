using ArchitecturalDreamMachineBackend.Constants;
using ArchitecturalDreamMachineBackend.Models;

namespace ArchitecturalDreamMachineBackend.Services
{
    /// <summary>
    /// Main service for generating geometric data that can be directly rendered by Three.js
    /// Combines VertexCalculator and FaceGenerator to produce complete GeometryData objects
    /// </summary>
    public class GeometryService : IGeometryService
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
            double pitchRatio = roofPitch / ArchitecturalConstants.PitchDivisor;
            
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
        /// Create parapet/railing geometries for flat roofs - fence style with posts and rails
        /// Returns multiple geometries forming a railing around the roof perimeter
        /// </summary>
        public List<GeometryData> CreateParapetWalls(
            double width,
            double depth,
            double overhang,
            double parapetHeight = ArchitecturalConstants.DefaultParapetHeight,
            double parapetThickness = ArchitecturalConstants.DefaultParapetThickness)
        {
            double roofWidth = width + (overhang * 2);
            double roofDepth = depth + (overhang * 2);
            
            var parapets = new List<GeometryData>();
            
            // Railing parameters
            double postSize = 0.25;       // Square post cross-section
            double railHeight = 0.15;     // Height of horizontal rails
            double railThickness = 0.1;   // Depth of rails
            double postSpacing = 4.0;     // Distance between posts
            double topRailY = parapetHeight - railHeight / 2;
            double midRailY = parapetHeight * 0.5;
            double bottomRailY = railHeight / 2 + 0.1;  // Slight gap from roof
            
            // Generate posts and rails for each edge
            // Front edge (Z+)
            AddRailingSegment(parapets, roofWidth, parapetHeight, 
                0, 0, roofDepth / 2, 0,
                postSize, postSpacing, railHeight, railThickness,
                topRailY, midRailY, bottomRailY);
            
            // Back edge (Z-)
            AddRailingSegment(parapets, roofWidth, parapetHeight,
                0, 0, -roofDepth / 2, 0,
                postSize, postSpacing, railHeight, railThickness,
                topRailY, midRailY, bottomRailY);
            
            // Right edge (X+)
            AddRailingSegment(parapets, roofDepth, parapetHeight,
                roofWidth / 2, 0, 0, Math.PI / 2,
                postSize, postSpacing, railHeight, railThickness,
                topRailY, midRailY, bottomRailY);
            
            // Left edge (X-)
            AddRailingSegment(parapets, roofDepth, parapetHeight,
                -roofWidth / 2, 0, 0, Math.PI / 2,
                postSize, postSpacing, railHeight, railThickness,
                topRailY, midRailY, bottomRailY);
            
            return parapets;
        }
        
        /// <summary>
        /// Add posts and rails for one edge of the railing
        /// </summary>
        private void AddRailingSegment(
            List<GeometryData> parapets,
            double length, double height,
            double centerX, double centerY, double centerZ,
            double rotationY,
            double postSize, double postSpacing,
            double railHeight, double railThickness,
            double topRailY, double midRailY, double bottomRailY)
        {
            var parapetIndices = Geometry.FaceGenerator.ParapetFaces();
            
            // Calculate number of posts needed
            int postCount = Math.Max(2, (int)Math.Ceiling(length / postSpacing) + 1);
            double actualSpacing = length / (postCount - 1);
            
            // Create posts
            var postVertices = Geometry.VertexCalculator.CalculateParapetVertices(postSize, height, postSize);
            for (int i = 0; i < postCount; i++)
            {
                double localX = -length / 2 + i * actualSpacing;
                
                // Calculate world position based on rotation
                double worldX, worldZ;
                if (Math.Abs(rotationY) < 0.01)
                {
                    worldX = centerX + localX;
                    worldZ = centerZ;
                }
                else
                {
                    worldX = centerX;
                    worldZ = centerZ + localX;
                }
                
                parapets.Add(new GeometryData
                {
                    Vertices = postVertices,
                    Indices = parapetIndices,
                    MaterialType = "metal",
                    Color = "#404040",
                    Position = new Position { X = worldX, Y = height / 2, Z = worldZ }
                });
            }
            
            // Create horizontal rails (top, middle, bottom)
            var topRailVertices = Geometry.VertexCalculator.CalculateParapetVertices(length, railHeight, railThickness);
            var midRailVertices = Geometry.VertexCalculator.CalculateParapetVertices(length, railHeight * 0.8, railThickness * 0.8);
            
            // Top rail
            parapets.Add(new GeometryData
            {
                Vertices = topRailVertices,
                Indices = parapetIndices,
                MaterialType = "metal",
                Color = "#505050",
                Position = new Position { X = centerX, Y = topRailY, Z = centerZ },
                Rotation = new Rotation { Y = rotationY }
            });
            
            // Middle rail
            parapets.Add(new GeometryData
            {
                Vertices = midRailVertices,
                Indices = parapetIndices,
                MaterialType = "metal",
                Color = "#484848",
                Position = new Position { X = centerX, Y = midRailY, Z = centerZ },
                Rotation = new Rotation { Y = rotationY }
            });
            
            // Bottom rail
            parapets.Add(new GeometryData
            {
                Vertices = midRailVertices,
                Indices = parapetIndices,
                MaterialType = "metal",
                Color = "#484848",
                Position = new Position { X = centerX, Y = bottomRailY, Z = centerZ },
                Rotation = new Rotation { Y = rotationY }
            });
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
