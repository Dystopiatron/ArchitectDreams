using ArchitecturalDreamMachineBackend.Models;

namespace ArchitecturalDreamMachineBackend.LayoutStrategies
{
    /// <summary>
    /// L-shaped building layout with two perpendicular wings
    /// PORTED FROM HouseViewer3D.js lines 498-502
    /// Main wing: full width × 60% depth
    /// Side wing: 50% width × 60% depth, offset to create L-shape
    /// </summary>
    public class LShapeLayoutStrategy : ILayoutStrategy
    {
        public LayoutData CalculateLayout(
            double footprintWidth,
            double footprintDepth,
            double ceilingHeight,
            int stories)
        {
            // L-shape dimensions (from original code)
            double mainWingWidth = footprintWidth;
            double mainWingDepth = footprintDepth * 0.6;
            
            double sideWingWidth = footprintWidth * 0.5;
            double sideWingDepth = footprintDepth * 0.6;
            
            double height = ceilingHeight * stories;
            
            var layout = new LayoutData
            {
                TotalWidth = footprintWidth,
                TotalDepth = footprintDepth,
                TotalHeight = height,
                Shape = "l-shape"
            };
            
            // Main wing (back section)
            layout.Sections.Add(new LayoutSection
            {
                Width = mainWingWidth,
                Height = height,
                Depth = mainWingDepth,
                X = 0,
                Y = height / 2,
                Z = -footprintDepth * 0.2,  // Offset toward back
                Floor = 1,
                AddWindows = true
            });
            
            // Side wing (front-right section)
            layout.Sections.Add(new LayoutSection
            {
                Width = sideWingWidth,
                Height = height,
                Depth = sideWingDepth,
                X = footprintWidth * 0.25,  // Offset to right
                Y = height / 2,
                Z = footprintDepth * 0.2,  // Offset toward front
                Floor = 1,
                AddWindows = true
            });
            
            // Two roof sections (one per wing)
            // Main wing roof
            layout.RoofSections.Add(new RoofSection
            {
                Width = mainWingWidth,
                Depth = mainWingDepth,
                X = 0,
                Y = height,
                Z = -footprintDepth * 0.2
            });
            
            // Side wing roof
            layout.RoofSections.Add(new RoofSection
            {
                Width = sideWingWidth,
                Depth = sideWingDepth,
                X = footprintWidth * 0.25,
                Y = height,
                Z = footprintDepth * 0.2
            });
            
            return layout;
        }
    }
}
