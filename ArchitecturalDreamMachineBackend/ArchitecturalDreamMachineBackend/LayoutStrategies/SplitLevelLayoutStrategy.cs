using ArchitecturalDreamMachineBackend.Models;

namespace ArchitecturalDreamMachineBackend.LayoutStrategies
{
    /// <summary>
    /// Split-level building layout
    /// Two sections at different heights for visual interest
    /// PORTED FROM HouseViewer3D.js lines 505-507
    /// </summary>
    public class SplitLevelLayoutStrategy : ILayoutStrategy
    {
        public LayoutData CalculateLayout(
            double footprintWidth,
            double footprintDepth,
            double ceilingHeight,
            int stories)
        {
            double lowerHeight = ceilingHeight * 0.7;
            double upperHeight = ceilingHeight * 0.5;
            double totalHeight = ceilingHeight * 1.2;
            
            var layout = new LayoutData
            {
                TotalWidth = footprintWidth,
                TotalDepth = footprintDepth,
                TotalHeight = totalHeight,
                Shape = "split-level"
            };
            
            // Lower level (70% height, 70% depth, full width)
            layout.Sections.Add(new LayoutSection
            {
                Width = footprintWidth,
                Height = lowerHeight,
                Depth = footprintDepth * 0.7,
                X = 0,
                Y = lowerHeight * 0.5,  // 0.35 * ceilingHeight
                Z = 0,
                Floor = 1,
                AddWindows = true
            });
            
            // Upper level (50% height, 60% width, 70% depth, offset right)
            layout.Sections.Add(new LayoutSection
            {
                Width = footprintWidth * 0.6,
                Height = upperHeight,
                Depth = footprintDepth * 0.7,
                X = footprintWidth * 0.2,  // Offset right
                Y = lowerHeight + upperHeight * 0.5,  // 0.85 * ceilingHeight
                Z = 0,
                Floor = 2,
                AddWindows = true
            });
            
            // Two roof sections at different heights
            layout.RoofSections.Add(new RoofSection
            {
                Width = footprintWidth,
                Depth = footprintDepth * 0.7,
                X = 0,
                Y = lowerHeight,
                Z = 0
            });
            
            layout.RoofSections.Add(new RoofSection
            {
                Width = footprintWidth * 0.6,
                Depth = footprintDepth * 0.7,
                X = footprintWidth * 0.2,
                Y = lowerHeight + upperHeight,
                Z = 0
            });
            
            return layout;
        }
    }
}
