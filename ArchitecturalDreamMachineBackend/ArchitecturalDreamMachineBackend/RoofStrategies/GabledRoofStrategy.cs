using ArchitecturalDreamMachineBackend.Models;
using ArchitecturalDreamMachineBackend.Services;

namespace ArchitecturalDreamMachineBackend.RoofStrategies
{
    /// <summary>
    /// Traditional gabled roof strategy
    /// PORTS Phase 1.1 fix from HouseViewer3D.js (lines 583-644)
    /// Creates ridge beam with two sloped planes and gable ends
    /// </summary>
    public class GabledRoofStrategy : IRoofStrategy
    {
        private readonly GeometryService _geometryService;
        
        public GabledRoofStrategy(GeometryService geometryService)
        {
            _geometryService = geometryService;
        }
        
        public RoofGeometry CalculateRoof(
            RoofSection section,
            double roofPitch,
            double overhang,
            bool hasParapet = false)
        {
            // Use GeometryService which has Phase 1.1 calculation logic
            var geometry = _geometryService.CreateGabledRoof(
                section.Width,
                section.Depth,
                roofPitch,
                overhang);
            
            // Calculate roof height (matches Phase 1.1 fix)
            double pitchRatio = roofPitch / 12.0;
            double roofWidth = section.Width + (overhang * 2);
            double roofHeight = (roofWidth / 2) * pitchRatio;
            
            // Position roof so its BASE (eaves level) is at top of building
            // Gabled roof vertices have eaves at Y=0 and ridge at Y=roofHeight
            // So we position the mesh at the top of the building (section.Y)
            geometry.Position = new Position
            {
                X = section.X,
                Y = section.Y,  // Top of building (eaves level)
                Z = section.Z
            };
            
            return new RoofGeometry
            {
                Geometry = geometry,
                Height = roofHeight,
                RoofType = "gabled",
                Pitch = roofPitch
            };
        }
    }
}
