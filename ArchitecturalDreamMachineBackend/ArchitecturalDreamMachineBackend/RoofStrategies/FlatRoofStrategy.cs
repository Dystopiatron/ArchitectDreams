using ArchitecturalDreamMachineBackend.Models;
using ArchitecturalDreamMachineBackend.Services;

namespace ArchitecturalDreamMachineBackend.RoofStrategies
{
    /// <summary>
    /// Flat roof strategy with optional parapet walls
    /// Common in Modern and Brutalist architecture
    /// </summary>
    public class FlatRoofStrategy : IRoofStrategy
    {
        private readonly GeometryService _geometryService;
        
        public FlatRoofStrategy(GeometryService geometryService)
        {
            _geometryService = geometryService;
        }
        
        public RoofGeometry CalculateRoof(
            RoofSection section,
            double roofPitch,
            double overhang,
            bool hasParapet = false)
        {
            // Create thin flat roof box
            double roofThickness = 0.75;
            var geometry = _geometryService.CreateFlatRoof(
                section.Width,
                section.Depth,
                overhang,
                roofThickness);
            
            // Position roof so its BOTTOM is at top of building
            // Roof geometry is centered at origin, so we need to offset by half thickness
            geometry.Position = new Position
            {
                X = section.X,
                Y = section.Y + (roofThickness / 2),  // Top of building + half roof thickness
                Z = section.Z
            };
            
            var roofGeometry = new RoofGeometry
            {
                Geometry = geometry,
                Height = 0.75,  // Thickness of flat roof
                RoofType = "flat",
                Pitch = 0
            };
            
            // Add parapet walls if specified (Brutalist style)
            if (hasParapet)
            {
                var parapets = _geometryService.CreateParapetWalls(
                    section.Width,
                    section.Depth,
                    overhang);
                
                // Adjust parapet positions to be on top of roof
                foreach (var parapet in parapets)
                {
                    if (parapet.Position != null)
                    {
                        parapet.Position.X += section.X;
                        parapet.Position.Y += section.Y;
                        parapet.Position.Z += section.Z;
                    }
                }
                
                // Store parapets in a way that can be retrieved
                // (For now, we'll need to handle this in orchestration)
                // This is a limitation of current structure - parapets are separate geometries
            }
            
            return roofGeometry;
        }
    }
}
