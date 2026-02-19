using ArchitecturalDreamMachineBackend.Models;

namespace ArchitecturalDreamMachineBackend.LayoutStrategies
{
    /// <summary>
    /// L-shaped building layout with two non-overlapping wings.
    /// Main wing: full width × back 60% of depth (Z ∈ [-0.5D, 0.1D])
    /// Corner wing: left 50% width × front 40% of depth (Z ∈ [0.1D, 0.5D])
    /// No geometric overlap, so no Z-fighting.
    /// </summary>
    public class LShapeLayoutStrategy : ILayoutStrategy
    {
        public LayoutData CalculateLayout(
            double footprintWidth,
            double footprintDepth,
            double ceilingHeight,
            int stories)
        {
            double height = ceilingHeight * stories;

            // Main wing spans full width, back 60% of depth
            double mainWingWidth = footprintWidth;
            double mainWingDepth = footprintDepth * 0.6;
            double mainWingCenterZ = -footprintDepth * 0.2;  // center of back 60%

            // Corner wing fills front-left: left 50% width × front 40% of depth
            // Front 40% of depth: Z ∈ [0.1D, 0.5D], center = 0.3D
            double cornerWingWidth = footprintWidth * 0.5;
            double cornerWingDepth = footprintDepth * 0.4;
            double cornerWingCenterX = -footprintWidth * 0.25; // left half
            double cornerWingCenterZ = footprintDepth * 0.3;   // front 40%

            var layout = new LayoutData
            {
                TotalWidth = footprintWidth,
                TotalDepth = footprintDepth,
                TotalHeight = height,
                Shape = "l-shape"
            };

            // Main wing (back section, full width)
            layout.Sections.Add(new LayoutSection
            {
                Width = mainWingWidth,
                Height = height,
                Depth = mainWingDepth,
                X = 0,
                Y = height / 2,
                Z = mainWingCenterZ,
                Floor = 1,
                AddWindows = true
            });

            // Corner wing (front-left, no overlap with main wing)
            layout.Sections.Add(new LayoutSection
            {
                Width = cornerWingWidth,
                Height = height,
                Depth = cornerWingDepth,
                X = cornerWingCenterX,
                Y = height / 2,
                Z = cornerWingCenterZ,
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
                Z = mainWingCenterZ
            });

            // Corner wing roof
            layout.RoofSections.Add(new RoofSection
            {
                Width = cornerWingWidth,
                Depth = cornerWingDepth,
                X = cornerWingCenterX,
                Y = height,
                Z = cornerWingCenterZ
            });
            
            return layout;
        }
    }
}
