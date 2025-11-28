using ArchitecturalDreamMachineBackend.Models;
using ArchitecturalDreamMachineBackend.RoofStrategies;

namespace ArchitecturalDreamMachineBackend.Services
{
    /// <summary>
    /// Service for calculating roof geometries
    /// Selects appropriate roof strategy based on roof type
    /// </summary>
    public class RoofService
    {
        private readonly ILogger<RoofService> _logger;
        private readonly GeometryService _geometryService;
        
        public RoofService(ILogger<RoofService> logger, GeometryService geometryService)
        {
            _logger = logger;
            _geometryService = geometryService;
        }
        
        /// <summary>
        /// Calculate roof geometries for all roof sections
        /// </summary>
        /// <param name="sections">List of roof sections to cover</param>
        /// <param name="roofType">Type of roof (gabled, flat, etc.)</param>
        /// <param name="roofPitch">Roof pitch (rise over 12)</param>
        /// <param name="overhang">Horizontal overhang</param>
        /// <param name="hasParapet">Whether to include parapet walls</param>
        /// <returns>List of roof geometries</returns>
        public List<RoofGeometry> CalculateRoofs(
            List<RoofSection> sections,
            string roofType,
            double roofPitch,
            double overhang,
            bool hasParapet)
        {
            _logger.LogInformation(
                "Calculating {Count} roof sections: type={Type}, pitch={Pitch}, overhang={Overhang}, parapet={Parapet}",
                sections.Count, roofType, roofPitch, overhang, hasParapet);
            
            // Select appropriate strategy
            IRoofStrategy strategy = SelectStrategy(roofType);
            
            // Calculate roof for each section
            var roofs = sections.Select(section => 
                strategy.CalculateRoof(section, roofPitch, overhang, hasParapet)
            ).ToList();
            
            _logger.LogInformation("Calculated {Count} roofs", roofs.Count);
            
            return roofs;
        }
        
        /// <summary>
        /// Calculate a single roof
        /// </summary>
        public RoofGeometry CalculateRoof(
            RoofSection section,
            string roofType,
            double roofPitch,
            double overhang,
            bool hasParapet)
        {
            IRoofStrategy strategy = SelectStrategy(roofType);
            return strategy.CalculateRoof(section, roofPitch, overhang, hasParapet);
        }
        
        /// <summary>
        /// Select appropriate roof strategy based on type
        /// </summary>
        private IRoofStrategy SelectStrategy(string roofType)
        {
            string type = (roofType ?? "flat").ToLower().Trim();
            
            return type switch
            {
                "gabled" => new GabledRoofStrategy(_geometryService),
                "flat" => new FlatRoofStrategy(_geometryService),
                _ => new FlatRoofStrategy(_geometryService) // Default to flat
            };
        }
    }
}
