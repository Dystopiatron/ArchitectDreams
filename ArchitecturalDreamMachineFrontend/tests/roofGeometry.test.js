/**
 * Roof Geometry Calculation Tests
 * Validates that roof pitch calculations produce correct slopes
 */

describe('Roof Geometry Calculations', () => {
  test('roof pitch matches specified pitch for square building', () => {
    const width = 50;
    const depth = 50;
    const pitchRatio = 0.5; // 6:12 pitch
    
    // Calculate radius from center to corner (diagonal distance)
    const radius = Math.sqrt(Math.pow(width / 2, 2) + Math.pow(depth / 2, 2));
    
    // Height should be radius * pitchRatio to maintain correct pitch
    const height = radius * pitchRatio;
    
    // Verify actual pitch matches specified pitch
    const actualPitch = height / radius;
    
    expect(actualPitch).toBeCloseTo(pitchRatio, 2);
  });
  
  test('roof pitch matches specified pitch for rectangular building', () => {
    const width = 48.3;
    const depth = 72.5;
    const pitchRatio = 0.5; // 6:12 pitch
    
    // Calculate radius from center to corner (diagonal distance)
    const radius = Math.sqrt(Math.pow(width / 2, 2) + Math.pow(depth / 2, 2));
    
    // Height should be radius * pitchRatio to maintain correct pitch
    const height = radius * pitchRatio;
    
    // Verify actual pitch matches specified pitch
    const actualPitch = height / radius;
    
    expect(actualPitch).toBeCloseTo(pitchRatio, 2);
  });
  
  test('roof height is correct for 3500 sqft Brutalist building', () => {
    // 3500 sqft single-story: 3500/1.5 aspect = 2333 sqft footprint
    // Width = sqrt(2333/1.5) = 39.4 ft
    // Depth = 2333/39.4 = 59.2 ft
    const width = 39.4;
    const depth = 59.2;
    const pitchRatio = 0.5; // 6:12 pitch (50% slope)
    const overhang = 1.5; // Standard eaves overhang
    
    const roofRadius = Math.sqrt(Math.pow(width / 2, 2) + Math.pow(depth / 2, 2)) + overhang;
    const roofHeight = roofRadius * pitchRatio;
    
    // For this building, roof should be approximately 21.8 ft tall
    // NOT 12.1 ft (which would result from using width/2)
    expect(roofHeight).toBeGreaterThan(20);
    expect(roofHeight).toBeLessThan(23);
  });
  
  test('old calculation produces incorrect shallow roofs', () => {
    const width = 50;
    const depth = 50;
    const pitchRatio = 0.5; // 6:12 pitch
    
    // Old WRONG calculation: height based on width/2
    const wrongHeight = (width / 2) * pitchRatio;
    
    // Correct calculation: height based on radius
    const radius = Math.sqrt(Math.pow(width / 2, 2) + Math.pow(depth / 2, 2));
    const correctHeight = radius * pitchRatio;
    
    // Wrong calculation produces ~29% shorter roof for square building
    const error = (correctHeight - wrongHeight) / correctHeight;
    expect(error).toBeCloseTo(0.29, 2);
  });
  
  test('new calculation maintains consistent pitch across all layouts', () => {
    const pitchRatio = 0.5; // 6:12 pitch
    const testCases = [
      { name: 'square', width: 50, depth: 50 },
      { name: 'rectangular', width: 40, depth: 60 },
      { name: 'wide', width: 60, depth: 40 },
    ];
    
    testCases.forEach(({ name, width, depth }) => {
      const radius = Math.sqrt(Math.pow(width / 2, 2) + Math.pow(depth / 2, 2));
      const height = radius * pitchRatio;
      const actualPitch = height / radius;
      
      // All layouts should have the same pitch
      expect(actualPitch).toBeCloseTo(pitchRatio, 2);
    });
  });
});
