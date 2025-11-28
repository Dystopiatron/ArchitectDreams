using ArchitecturalDreamMachineBackend.Models;

namespace ArchitecturalDreamMachineBackend.LayoutStrategies
{
    /// <summary>
    /// Simple rectangular building layout
    /// Single box with optional multiple stories
    /// </summary>
    public class CubeLayoutStrategy : ILayoutStrategy
    {
        public LayoutData CalculateLayout(
            double footprintWidth,
            double footprintDepth,
            double ceilingHeight,
            int stories)
        {
            var layout = new LayoutData
            {
                TotalWidth = footprintWidth,
                TotalDepth = footprintDepth,
                TotalHeight = ceilingHeight * stories,
                Shape = "cube"
            };
            
            // Single building section spanning all stories
            layout.Sections.Add(new LayoutSection
            {
                Width = footprintWidth,
                Height = ceilingHeight * stories,
                Depth = footprintDepth,
                X = 0,
                Y = (ceilingHeight * stories) / 2,  // Center vertically
                Z = 0,
                Floor = 1,
                AddWindows = true
            });
            
            // Single roof section covering entire building
            layout.RoofSections.Add(new RoofSection
            {
                Width = footprintWidth,
                Depth = footprintDepth,
                X = 0,
                Y = ceilingHeight * stories,  // Top of building
                Z = 0
            });
            
            return layout;
        }
    }
}
