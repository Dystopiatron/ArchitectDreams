using ArchitecturalDreamMachineBackend.Models;

namespace ArchitecturalDreamMachineBackend.LayoutStrategies
{
    /// <summary>
    /// Split-level building layout.
    /// One section per story, each with full ceiling height.
    /// Odd floors (1, 3, 5…) use full footprint width.
    /// Even floors (2, 4, 6…) use 60% width, offset right by 0.2W.
    /// All sections share 70% footprint depth.
    /// </summary>
    public class SplitLevelLayoutStrategy : ILayoutStrategy
    {
        public LayoutData CalculateLayout(
            double footprintWidth,
            double footprintDepth,
            double ceilingHeight,
            int stories)
        {
            double depth = footprintDepth * 0.7;
            double totalHeight = stories * ceilingHeight;

            var layout = new LayoutData
            {
                TotalWidth = footprintWidth,
                TotalDepth = footprintDepth,
                TotalHeight = totalHeight,
                Shape = "split-level"
            };

            for (int floor = 1; floor <= stories; floor++)
            {
                bool isWide = floor % 2 == 1; // odd floors are wide
                double sectionWidth = isWide ? footprintWidth : footprintWidth * 0.6;
                double sectionX = isWide ? 0 : footprintWidth * 0.2;
                double baseY = (floor - 1) * ceilingHeight;

                layout.Sections.Add(new LayoutSection
                {
                    Width = sectionWidth,
                    Height = ceilingHeight,
                    Depth = depth,
                    X = sectionX,
                    Y = baseY + ceilingHeight / 2,
                    Z = 0,
                    Floor = floor,
                    AddWindows = true
                });

                layout.RoofSections.Add(new RoofSection
                {
                    Width = sectionWidth,
                    Depth = depth,
                    X = sectionX,
                    Y = baseY + ceilingHeight,
                    Z = 0
                });
            }

            return layout;
        }
    }
}
