namespace ArchitecturalDreamMachineBackend.Geometry
{
    /// <summary>
    /// Calculates vertex positions for various geometric shapes
    /// Vertices are returned as flat arrays [x,y,z, x,y,z, ...]
    /// </summary>
    public static class VertexCalculator
    {
        /// <summary>
        /// Calculate 8 vertices for a box centered at origin
        /// </summary>
        /// <param name="width">Width (X dimension)</param>
        /// <param name="height">Height (Y dimension)</param>
        /// <param name="depth">Depth (Z dimension)</param>
        /// <returns>24 floats representing 8 vertices (x,y,z each)</returns>
        public static float[] CalculateBoxVertices(double width, double height, double depth)
        {
            float hw = (float)(width / 2);
            float hh = (float)(height / 2);
            float hd = (float)(depth / 2);
            
            return new float[]
            {
                // Front face (Z+)
                -hw, -hh, hd,  // 0: bottom-left-front
                hw, -hh, hd,   // 1: bottom-right-front
                hw, hh, hd,    // 2: top-right-front
                -hw, hh, hd,   // 3: top-left-front
                
                // Back face (Z-)
                -hw, -hh, -hd, // 4: bottom-left-back
                hw, -hh, -hd,  // 5: bottom-right-back
                hw, hh, -hd,   // 6: top-right-back
                -hw, hh, -hd   // 7: top-left-back
            };
        }
        
        /// <summary>
        /// Calculate 6 vertices for a traditional gabled roof
        /// PORTED FROM Phase 1.1 fix in HouseViewer3D.js (lines 583-644)
        /// </summary>
        /// <param name="width">Building width (roof spans this dimension)</param>
        /// <param name="depth">Building depth (ridge runs this direction)</param>
        /// <param name="roofHeight">Height of ridge above eaves</param>
        /// <param name="overhang">Horizontal overhang beyond walls</param>
        /// <returns>18 floats representing 6 vertices</returns>
        public static float[] CalculateGabledRoofVertices(
            double width, 
            double depth, 
            double roofHeight, 
            double overhang)
        {
            // Apply overhang to extend roof beyond walls on all sides
            float roofWidth = (float)(width + (overhang * 2));
            float roofDepth = (float)(depth + (overhang * 2));
            
            float hw = roofWidth / 2;   // half width (left/right from center)
            float hd = roofDepth / 2;   // half depth (front/back from center)
            float rh = (float)roofHeight;
            
            return new float[]
            {
                // Ridge beam (top center, front to back along Z-axis)
                0, rh, hd,      // 0: Ridge front
                0, rh, -hd,     // 1: Ridge back
                
                // Base corners (eaves level)
                -hw, 0, hd,     // 2: Left front eave
                hw, 0, hd,      // 3: Right front eave
                hw, 0, -hd,     // 4: Right back eave
                -hw, 0, -hd     // 5: Left back eave
            };
        }
        
        /// <summary>
        /// Calculate 8 vertices for a flat roof (thin box)
        /// </summary>
        public static float[] CalculateFlatRoofVertices(double width, double depth, double thickness, double overhang)
        {
            float roofWidth = (float)(width + (overhang * 2));
            float roofDepth = (float)(depth + (overhang * 2));
            float hw = roofWidth / 2;
            float hd = roofDepth / 2;
            float ht = (float)(thickness / 2);
            
            return new float[]
            {
                // Bottom face
                -hw, -ht, hd,
                hw, -ht, hd,
                hw, -ht, -hd,
                -hw, -ht, -hd,
                
                // Top face
                -hw, ht, hd,
                hw, ht, hd,
                hw, ht, -hd,
                -hw, ht, -hd
            };
        }
        
        /// <summary>
        /// Calculate vertices for a parapet wall (rectangular box on edge)
        /// </summary>
        public static float[] CalculateParapetVertices(double length, double height, double thickness)
        {
            float hl = (float)(length / 2);
            float h = (float)height;
            float ht = (float)(thickness / 2);
            
            return new float[]
            {
                // Bottom face
                -hl, 0, -ht,
                hl, 0, -ht,
                hl, 0, ht,
                -hl, 0, ht,
                
                // Top face
                -hl, h, -ht,
                hl, h, -ht,
                hl, h, ht,
                -hl, h, ht
            };
        }
    }
}
