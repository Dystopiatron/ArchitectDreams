using ArchitecturalDreamMachineBackend.Models;

namespace ArchitecturalDreamMachineBackend.LayoutStrategies
{
    /// <summary>
    /// Angled/rotated modern design layout
    /// Main tower: single section spanning all stories (70% footprint size).
    /// Angled wing: single first-floor addition offset from the tower (50% size).
    /// Single-section tower removes per-floor coplanar side-wall Z-fighting.
    /// Note: The wing is geometrically a shifted box; an actual 30° rotation would
    /// require frontend support that is not currently implemented.
    /// </summary>
    public class AngledLayoutStrategy : ILayoutStrategy
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
                Shape = "angled"
            };

            // Main tower — single section spanning all stories (70% size)
            layout.Sections.Add(new LayoutSection
            {
                Width = footprintWidth * 0.7,
                Height = totalHeight,
                Depth = footprintDepth * 0.7,
                X = 0,
                Y = totalHeight / 2,
                Z = 0,
                Floor = 1,
                AddWindows = true
            });

            // Angled wing on first floor only (50% size, offset to front-right)
            layout.Sections.Add(new LayoutSection
            {
                Width = footprintWidth * 0.5,
                Height = ceilingHeight,
                Depth = footprintDepth * 0.5,
                X = footprintWidth * 0.4,
                Y = ceilingHeight * 0.5,
                Z = footprintDepth * 0.4,
                Floor = 1,
                AddWindows = true
            });

            // Roof on main tower
            layout.RoofSections.Add(new RoofSection
            {
                Width = footprintWidth * 0.7,
                Depth = footprintDepth * 0.7,
                X = 0,
                Y = totalHeight,
                Z = 0
            });

            // Roof on angled wing
            layout.RoofSections.Add(new RoofSection
            {
                Width = footprintWidth * 0.5,
                Depth = footprintDepth * 0.5,
                X = footprintWidth * 0.4,
                Y = ceilingHeight,
                Z = footprintDepth * 0.4
            });

            return layout;
        }
    }
}
