namespace ArchitecturalDreamMachineBackend.Geometry
{
    /// <summary>
    /// Generates face indices for geometric shapes
    /// Indices define triangular faces by referencing vertex positions
    /// </summary>
    public static class FaceGenerator
    {
        /// <summary>
        /// Generate 36 indices (12 triangles) for a box with 8 vertices
        /// Each face is 2 triangles (6 indices)
        /// </summary>
        /// <returns>36 indices forming 12 triangles (6 faces × 2 triangles)</returns>
        public static int[] BoxFaces()
        {
            return new int[]
            {
                // Front face (Z+)
                0, 1, 2,  // Triangle 1
                0, 2, 3,  // Triangle 2
                
                // Back face (Z-)
                5, 4, 7,  // Triangle 1
                5, 7, 6,  // Triangle 2
                
                // Top face (Y+)
                3, 2, 6,  // Triangle 1
                3, 6, 7,  // Triangle 2
                
                // Bottom face (Y-)
                4, 5, 1,  // Triangle 1
                4, 1, 0,  // Triangle 2
                
                // Right face (X+)
                1, 5, 6,  // Triangle 1
                1, 6, 2,  // Triangle 2
                
                // Left face (X-)
                4, 0, 3,  // Triangle 1
                4, 3, 7   // Triangle 2
            };
        }
        
        /// <summary>
        /// Generate 24 indices (8 triangles) for a gabled roof with 6 vertices
        /// PORTED FROM Phase 1.1 fix in HouseViewer3D.js
        /// - 2 roof slopes (4 triangles total)
        /// - 2 gable ends (2 triangles total)
        /// </summary>
        /// <returns>24 indices forming 8 triangles</returns>
        public static int[] GabledRoofFaces()
        {
            return new int[]
            {
                // Left roof slope (from ridge to left eave)
                0, 2, 5,    // Front half of left slope
                0, 5, 1,    // Back half of left slope
                
                // Right roof slope (from ridge to right eave)
                0, 3, 4,    // Front half of right slope
                0, 4, 1,    // Back half of right slope
                
                // Front gable (triangular end)
                0, 3, 2,    // Front triangle
                
                // Back gable (triangular end)
                1, 5, 4     // Back triangle
            };
        }
        
        /// <summary>
        /// Generate indices for flat roof (same as box, 12 triangles)
        /// </summary>
        public static int[] FlatRoofFaces()
        {
            return BoxFaces();
        }
        
        /// <summary>
        /// Generate indices for parapet wall (box faces)
        /// </summary>
        public static int[] ParapetFaces()
        {
            return BoxFaces();
        }
        
        /// <summary>
        /// Generate indices for a simple quad (2 triangles)
        /// Useful for windows, doors, interior walls
        /// </summary>
        /// <returns>6 indices forming 2 triangles</returns>
        public static int[] QuadFaces()
        {
            return new int[]
            {
                0, 1, 2,  // First triangle
                0, 2, 3   // Second triangle
            };
        }
    }
}
