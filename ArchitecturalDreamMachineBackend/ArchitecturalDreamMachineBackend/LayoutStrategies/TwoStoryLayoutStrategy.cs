using ArchitecturalDreamMachineBackend.Models;

namespace ArchitecturalDreamMachineBackend.LayoutStrategies
{
    /// <summary>
    /// Two-story building layout
    /// Single rectangular footprint, multiple floors stacked
    /// </summary>
    public class TwoStoryLayoutStrategy : ILayoutStrategy
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
                Shape = "two-story"
            };
            
            // Create section for each floor
            for (int floor = 1; floor <= stories; floor++)
            {
                double floorY = (floor - 0.5) * ceilingHeight;
                
                layout.Sections.Add(new LayoutSection
                {
                    Width = footprintWidth,
                    Height = ceilingHeight,
                    Depth = footprintDepth,
                    X = 0,
                    Y = floorY,
                    Z = 0,
                    Floor = floor,
                    AddWindows = true
                });
            }
            
            // Single roof on top
            layout.RoofSections.Add(new RoofSection
            {
                Width = footprintWidth,
                Depth = footprintDepth,
                X = 0,
                Y = ceilingHeight * stories,
                Z = 0
            });
            
            return layout;
        }
    }
}
