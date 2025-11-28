using ArchitecturalDreamMachineBackend.Models;

namespace ArchitecturalDreamMachineBackend.LayoutStrategies
{
    /// <summary>
    /// Angled/rotated modern design layout
    /// Main tower with angled wing for architectural interest
    /// PORTED FROM HouseViewer3D.js lines 510-523
    /// Note: Rotation handled by frontend rendering, backend just provides dimensions
    /// </summary>
    public class AngledLayoutStrategy : ILayoutStrategy
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
                Shape = "angled"
            };
            
            // Main vertical tower - stacked floors (70% size)
            for (int floor = 1; floor <= stories; floor++)
            {
                double floorY = (floor - 0.5) * ceilingHeight;
                
                layout.Sections.Add(new LayoutSection
                {
                    Width = footprintWidth * 0.7,
                    Height = ceilingHeight,
                    Depth = footprintDepth * 0.7,
                    X = 0,
                    Y = floorY,
                    Z = 0,
                    Floor = floor,
                    AddWindows = true
                });
            }
            
            // Angled wing on first floor only (50% size, offset)
            // Note: Rotation by 30 degrees (Math.PI / 6) should be handled in frontend
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
                Y = ceilingHeight * stories,
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
