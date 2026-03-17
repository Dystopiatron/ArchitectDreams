using ArchitecturalDreamMachineBackend.Models;

namespace ArchitecturalDreamMachineBackend.LayoutStrategies
{
    /// <summary>
    /// Split-level building layout.
    /// Main section: 2 stories, 50% of width on right side.
    /// Side wing: 1 story, 50% of width on left side (garage/family room).
    /// Sections touch at X=0, each extending to the footprint edge. Capped at 2 stories max.
    /// </summary>
    public class SplitLevelLayoutStrategy : ILayoutStrategy
    {
        public LayoutData CalculateLayout(
            double footprintWidth,
            double footprintDepth,
            double ceilingHeight,
            int stories)
        {
            // Cap at 2 stories for split-level
            int effectiveStories = Math.Min(stories, 2);
            double mainHeight = effectiveStories * ceilingHeight;
            double wingHeight = ceilingHeight; // Always 1 story

            var layout = new LayoutData
            {
                TotalWidth = footprintWidth,
                TotalDepth = footprintDepth,
                TotalHeight = mainHeight,
                Shape = "split-level"
            };

            // Main section: 50% width on right side (X from 0 to 0.5W)
            // This aligns right edge with footprint right edge (0.5W in building coords)
            double mainWidth = footprintWidth * 0.5;
            double mainX = mainWidth / 2; // Center at 0.25W, edges at 0 and 0.5W

            layout.Sections.Add(new LayoutSection
            {
                Width = mainWidth,
                Height = mainHeight,
                Depth = footprintDepth,
                X = mainX,
                Y = mainHeight / 2,
                Z = 0,
                Floor = 1,
                AddWindows = true
            });

            // Side wing: 50% width on left side (X from -0.5W to 0)
            // This aligns left edge with footprint left edge (-0.5W in building coords)
            double wingWidth = footprintWidth * 0.5;
            double wingX = -wingWidth / 2; // Center at -0.25W, edges at -0.5W and 0

            layout.Sections.Add(new LayoutSection
            {
                Width = wingWidth,
                Height = wingHeight,
                Depth = footprintDepth,
                X = wingX,
                Y = wingHeight / 2,
                Z = 0,
                Floor = 1,
                AddWindows = true
            });

            // Roof on main section
            layout.RoofSections.Add(new RoofSection
            {
                Width = mainWidth,
                Depth = footprintDepth,
                X = mainX,
                Y = mainHeight,
                Z = 0
            });

            // Roof on side wing
            layout.RoofSections.Add(new RoofSection
            {
                Width = wingWidth,
                Depth = footprintDepth,
                X = wingX,
                Y = wingHeight,
                Z = 0
            });

            return layout;
        }
    }
}
