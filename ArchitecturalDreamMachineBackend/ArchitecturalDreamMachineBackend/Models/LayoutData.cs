namespace ArchitecturalDreamMachineBackend.Models
{
    /// <summary>
    /// Represents a single building section (wing, floor, etc.)
    /// </summary>
    public class LayoutSection
    {
        /// <summary>
        /// Width of this section (X dimension)
        /// </summary>
        public double Width { get; set; }
        
        /// <summary>
        /// Height of this section (Y dimension)
        /// </summary>
        public double Height { get; set; }
        
        /// <summary>
        /// Depth of this section (Z dimension)
        /// </summary>
        public double Depth { get; set; }
        
        /// <summary>
        /// X position offset from building center
        /// </summary>
        public double X { get; set; }
        
        /// <summary>
        /// Y position offset (typically height/2 for centered box)
        /// </summary>
        public double Y { get; set; }
        
        /// <summary>
        /// Z position offset from building center
        /// </summary>
        public double Z { get; set; }
        
        /// <summary>
        /// Floor number (1-based)
        /// </summary>
        public int Floor { get; set; }
        
        /// <summary>
        /// Whether this section should have windows
        /// </summary>
        public bool AddWindows { get; set; } = true;
    }
    
    /// <summary>
    /// Represents where a roof should be placed
    /// </summary>
    public class RoofSection
    {
        /// <summary>
        /// Width of roof coverage (X dimension)
        /// </summary>
        public double Width { get; set; }
        
        /// <summary>
        /// Depth of roof coverage (Z dimension)
        /// </summary>
        public double Depth { get; set; }
        
        /// <summary>
        /// X position offset
        /// </summary>
        public double X { get; set; }
        
        /// <summary>
        /// Y position (typically top of building)
        /// </summary>
        public double Y { get; set; }
        
        /// <summary>
        /// Z position offset
        /// </summary>
        public double Z { get; set; }
    }
    
    /// <summary>
    /// Complete layout data for a building
    /// Defines all sections and where roofs should be placed
    /// </summary>
    public class LayoutData
    {
        /// <summary>
        /// All building sections (walls, wings, floors)
        /// </summary>
        public List<LayoutSection> Sections { get; set; } = new();
        
        /// <summary>
        /// Where roofs should be placed
        /// </summary>
        public List<RoofSection> RoofSections { get; set; } = new();
        
        /// <summary>
        /// Total width of entire building
        /// </summary>
        public double TotalWidth { get; set; }
        
        /// <summary>
        /// Total depth of entire building
        /// </summary>
        public double TotalDepth { get; set; }
        
        /// <summary>
        /// Total height of building (excluding roof)
        /// </summary>
        public double TotalHeight { get; set; }
        
        /// <summary>
        /// Building shape identifier
        /// </summary>
        public string Shape { get; set; } = "cube";
    }
}
