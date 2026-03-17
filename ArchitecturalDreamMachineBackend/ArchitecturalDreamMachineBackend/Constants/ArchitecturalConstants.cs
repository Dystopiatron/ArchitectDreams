namespace ArchitecturalDreamMachineBackend.Constants;

/// <summary>
/// Centralized architectural constants used throughout the application
/// These values define default measurements and ratios for building generation
/// </summary>
public static class ArchitecturalConstants
{
    // ===================
    // Footprint Calculations
    // ===================
    
    /// <summary>
    /// Default width-to-depth ratio for rectangular footprints (1.5:1)
    /// </summary>
    public const double DefaultAspectRatio = 1.5;
    
    /// <summary>
    /// Height-to-base ratio for proportional building height
    /// </summary>
    public const double HeightToBaseRatio = 0.6;
    
    // ===================
    // Room Layout Ratios
    // ===================
    
    /// <summary>
    /// Primary room dimension ratio (60% of building dimension)
    /// </summary>
    public const double PrimaryRoomRatio = 0.6;
    
    /// <summary>
    /// Secondary room dimension ratio (50% of building dimension)
    /// </summary>
    public const double SecondaryRoomRatio = 0.5;
    
    /// <summary>
    /// Tertiary room dimension ratio (70% of building dimension)
    /// </summary>
    public const double TertiaryRoomRatio = 0.7;
    
    // ===================
    // Roof Constants
    // ===================
    
    /// <summary>
    /// Divisor for roof pitch calculation (pitch is expressed as rise over 12)
    /// </summary>
    public const double PitchDivisor = 12.0;
    
    /// <summary>
    /// Default roof pitch (6:12 slope)
    /// </summary>
    public const double DefaultRoofPitch = 6.0;
    
    /// <summary>
    /// Default eaves overhang in feet
    /// </summary>
    public const double DefaultEavesOverhang = 1.5;
    
    // ===================
    // Window Constants
    // ===================
    
    /// <summary>
    /// Default window area in square feet (3ft x 4ft window)
    /// </summary>
    public const double DefaultWindowArea = 12.0;
    
    /// <summary>
    /// Default window-to-wall ratio (15%)
    /// </summary>
    public const double DefaultWindowToWallRatio = 0.15;
    
    /// <summary>
    /// Minimum windows per room
    /// </summary>
    public const int MinWindowsPerRoom = 1;
    
    /// <summary>
    /// Maximum windows per room (raised to allow style differentiation)
    /// </summary>
    public const int MaxWindowsPerRoom = 12;
    
    // ===================
    // Height Constants
    // ===================
    
    /// <summary>
    /// Default ceiling height in feet
    /// </summary>
    public const double DefaultCeilingHeight = 9.0;
    
    // ===================
    // Parapet Constants (Flat Roofs)
    // ===================
    
    /// <summary>
    /// Default parapet height in feet
    /// </summary>
    public const double DefaultParapetHeight = 2.5;
    
    /// <summary>
    /// Default parapet wall thickness in feet
    /// </summary>
    public const double DefaultParapetThickness = 0.5;
}
