using ArchitecturalDreamMachineBackend.Models;

namespace ArchitecturalDreamMachineBackend.LayoutStrategies
{
    /// <summary>
    /// Two-story building layout
    /// Single rectangular footprint, all stories in one section to avoid
    /// per-floor coplanar side-wall Z-fighting.
    /// Story count is still honoured (affects total height and window placement).
    /// </summary>
    public class TwoStoryLayoutStrategy : ILayoutStrategy
    {
        public LayoutData CalculateLayout(
            double footprintWidth,
            double footprintDepth,
            double ceilingHeight,
            int stories)
        {
            double totalHeight = ceilingHeight * stories;

            var layout = new LayoutData
            {
                TotalWidth = footprintWidth,
                TotalDepth = footprintDepth,
                TotalHeight = totalHeight,
                Shape = "two-story"
            };

            // Single section spanning all stories — no coplanar side walls
            layout.Sections.Add(new LayoutSection
            {
                Width = footprintWidth,
                Height = totalHeight,
                Depth = footprintDepth,
                X = 0,
                Y = totalHeight / 2,
                Z = 0,
                Floor = 1,
                AddWindows = true
            });

            // Single roof on top
            layout.RoofSections.Add(new RoofSection
            {
                Width = footprintWidth,
                Depth = footprintDepth,
                X = 0,
                Y = totalHeight,
                Z = 0
            });

            return layout;
        }
    }
}
