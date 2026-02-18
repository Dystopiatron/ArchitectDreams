# Windows and Doors Implementation Plan

## Prerequisites
- ✅ All 15 test cases from TESTING_PLAN.md completed and passing
- ✅ All layouts rendering correctly (Cube, L-Shape, Two-Story, Angled, Split-Level)
- ✅ Roof positioning verified for 1, 2, and 3 story buildings
- ✅ Camera controls tested and working
- ✅ No major geometry or rendering bugs

## Overview
This implementation adds realistic window and door placement to all architectural designs. Windows and doors should be automatically positioned based on wall dimensions, architectural style, and building parameters.

## Design Requirements

### Door Specifications
1. **Main Entrance Door**
   - Height: 2.1m (7 feet)
   - Width: 0.9m (3 feet) for single door, 1.8m for double door
   - Position: Ground floor, centered on front-facing wall
   - Clearance: Minimum 0.5m from wall corners
   - Depth: Recessed 0.05m into wall for visual depth

2. **Interior Doors** (between rooms)
   - Height: 2.0m
   - Width: 0.8m (standard interior door)
   - Position: Centered in interior walls connecting rooms
   - Frequency: At least one door per enclosed room

3. **Back/Side Doors** (optional, style-dependent)
   - Height: 2.0m
   - Width: 0.9m
   - Position: Back or side walls for multi-story or larger homes
   - Condition: Only for buildings > 150 sq meters or 2+ stories

### Window Specifications
1. **Standard Windows**
   - Height: 1.2m
   - Width: 1.0m (single), 1.5m (double), 2.0m (triple)
   - Sill Height: 0.9m from floor level
   - Spacing: Evenly distributed along exterior walls
   - Minimum spacing: 1.0m between windows
   - Clearance: 0.5m from corners, 0.5m from doors

2. **Window Placement Rules**
   - Living areas: Larger windows (1.5m-2.0m wide)
   - Bedrooms: Standard windows (1.0m-1.5m wide)
   - Bathrooms: Smaller windows (0.6m wide) positioned higher (1.5m sill)
   - Front facade: Symmetrical window placement when possible
   - Side/back walls: Functional placement based on wall length

3. **Story-Specific Rules**
   - Ground floor: Sill at 0.9m
   - Second floor: Sill at 0.9m from second floor level
   - Third floor: Sill at 0.9m from third floor level
   - Ensure windows don't overlap between floors

### Architectural Style Variations
Different styles should influence window/door design:

- **Modern**: Large floor-to-ceiling windows, glass doors, minimal frames
- **Traditional**: Standard rectangular windows, decorative door frames
- **Colonial**: Multi-pane windows, prominent front door
- **Contemporary**: Varied window sizes, asymmetric placement
- **Mediterranean**: Arched windows and doors, smaller openings
- **Industrial**: Large windows, metal-framed doors
- **Minimalist**: Clean lines, uniform window sizes

## Implementation Strategy

### Phase 1: Backend Geometry Generation

#### 1.1 Create Door/Window Models
**File**: `ArchitecturalDreamMachineBackend/Geometry/Opening.cs`

```csharp
public class Opening
{
    public enum OpeningType { Door, Window }
    public enum DoorType { Main, Interior, Back, Sliding }
    public enum WindowType { Standard, Large, Small, FloorToCeiling }
    
    public OpeningType Type { get; set; }
    public Vector3 Position { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public float Depth { get; set; }
    public Vector3 Normal { get; set; } // Wall facing direction
    public string Style { get; set; }
    
    // Window-specific
    public float SillHeight { get; set; }
    
    // Door-specific
    public bool IsDoubleDoor { get; set; }
    public float SwingAngle { get; set; }
}
```

#### 1.2 Create Opening Generator Service
**File**: `ArchitecturalDreamMachineBackend/Services/OpeningGenerator.cs`

Key methods:
- `GenerateMainDoor(Wall frontWall, string style)` - Place main entrance
- `GenerateWindows(List<Wall> exteriorWalls, HouseParameters params, string style)` - Distribute windows
- `GenerateInteriorDoors(List<Wall> interiorWalls, LayoutStrategy layout)` - Connect rooms
- `CalculateWindowSpacing(float wallLength, string style)` - Determine window count and positions
- `ValidateOpeningPlacement(Opening opening, List<Opening> existing)` - Check for conflicts
- `GenerateOpeningGeometry(Opening opening)` - Create mesh for rendering

#### 1.3 Integrate with Existing Geometry Pipeline
**Files to modify**:
- `Services/GeometryGeneratorService.cs` - Add opening generation step
- `Geometry/Mesh.cs` - Add method to cut openings from walls
- `Geometry/Wall.cs` - Track openings in each wall

**Integration points**:
1. After walls are generated, before roof
2. Cut rectangular holes in wall geometry for each opening
3. Generate door/window frame geometry
4. Add door/window materials (glass, wood, metal)

### Phase 2: Opening Placement Logic

#### 2.1 Wall Analysis
For each exterior wall:
1. Calculate wall dimensions and orientation
2. Identify wall type (front, back, left, right)
3. Calculate available space (minus corners and existing openings)
4. Determine appropriate opening types for wall position

#### 2.2 Opening Distribution Algorithm
```
For each exterior wall:
  1. If front wall AND ground floor: Place main door centered
  2. Calculate remaining wall segments after door placement
  3. For each segment:
     - Determine max windows that fit (accounting for spacing)
     - Space windows evenly
     - Respect corner clearances
  4. For upper floors: Align windows with ground floor when possible
  5. Validate no overlapping openings

For interior walls:
  1. Identify room connections
  2. Place interior doors at connection points
  3. Center doors in wall segments
```

#### 2.3 Style-Based Modifications
Create style profiles that modify default behavior:
- Window size multipliers
- Door ornament complexity
- Frame thickness
- Glass properties (transparency, reflection)
- Spacing preferences

### Phase 3: Geometry Cutting and Frame Generation

#### 3.1 Wall Opening Cutting
Modify wall mesh generation to:
1. Detect openings that intersect with wall plane
2. Subdivide wall geometry around opening
3. Create inner edges for opening perimeter
4. Generate UV coordinates for wall sections

#### 3.2 Frame and Detail Generation
For each opening, generate:
1. **Frame mesh**: Rectangular border around opening
   - Width: 0.05m-0.1m depending on style
   - Depth: Protrudes 0.02m from wall
   - Material: Wood or metal based on style

2. **Door mesh**: 
   - Solid panel with optional glass insert
   - Handle/knob at appropriate height
   - Optional: hinges, decorative elements

3. **Window mesh**:
   - Glass pane (semi-transparent material)
   - Window dividers (muntins) for traditional styles
   - Optional: shutters, sills, trim

### Phase 4: Material and Rendering

#### 4.1 New Materials
**File**: `Geometry/Material.cs` - Add new material types:
- Glass (transparency: 0.7, reflectivity: 0.3)
- Wood (for doors and frames)
- Metal (for modern door frames)
- Window trim

#### 4.2 Frontend Rendering Updates
**File**: `ArchitecturalDreamMachineFrontend/renderers/MaterialManager.js`

Add materials:
```javascript
createGlassMaterial() {
  return new THREE.MeshPhysicalMaterial({
    color: 0xaaccff,
    transparent: true,
    opacity: 0.3,
    roughness: 0.1,
    metalness: 0.1,
    transmission: 0.9,
    thickness: 0.01
  });
}

createDoorMaterial(style) {
  // Wood or metal based on style
  return new THREE.MeshStandardMaterial({
    color: style === 'modern' ? 0x333333 : 0x8b4513,
    roughness: 0.7,
    metalness: style === 'modern' ? 0.5 : 0.1
  });
}
```

### Phase 5: Testing and Validation

#### 5.1 Test Cases
Create new test file: `Tests/TestOpeningGeneration.cs`

Test scenarios:
1. Single-story cube: 4 walls, verify window distribution
2. Two-story L-shape: Check window alignment between floors
3. Split-level: Verify windows at different elevations
4. Angled layout: Windows follow wall angles correctly
5. Main door always on front wall
6. Interior doors connect rooms appropriately
7. No overlapping openings
8. All openings within wall boundaries
9. Style variations produce different results
10. Small buildings have fewer windows than large ones

#### 5.2 Visual Validation Checklist
Using the frontend 3D viewer:
- [ ] Doors are visible and properly positioned
- [ ] Windows distributed evenly on all exterior walls
- [ ] Upper floor windows align with lower floor (when appropriate)
- [ ] Glass material is transparent
- [ ] Frames are visible and proportional
- [ ] No z-fighting or geometry artifacts
- [ ] Openings don't intersect with roof
- [ ] Interior doors visible in appropriate walls
- [ ] Different styles produce visually distinct results

### Phase 6: API and Data Model Updates

#### 6.1 Database Schema
**File**: `Data/Design.cs` - Add properties:
```csharp
public bool IncludeWindows { get; set; } = true;
public bool IncludeDoors { get; set; } = true;
public string WindowStyle { get; set; } = "Standard";
public string DoorStyle { get; set; } = "Standard";
```

Create migration:
```bash
cd ArchitecturalDreamMachineBackend/ArchitecturalDreamMachineBackend
dotnet ef migrations add AddWindowsAndDoors
dotnet ef database update
```

#### 6.2 API Response
**File**: `Models/GeometryData.cs` - Add:
```csharp
public class OpeningData
{
    public string Type { get; set; } // "door" or "window"
    public List<float> Position { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public string MaterialType { get; set; }
}

// Add to GeometryData:
public List<OpeningData> Openings { get; set; }
```

#### 6.3 Frontend Input Controls
**File**: `ArchitecturalDreamMachineFrontend/screens/MainScreen.js`

Add toggles:
```javascript
<Text style={styles.label}>Include Windows</Text>
<Switch value={includeWindows} onValueChange={setIncludeWindows} />

<Text style={styles.label}>Include Doors</Text>
<Switch value={includeDoors} onValueChange={setIncludeDoors} />
```

## Implementation Order

1. **Day 1**: Create Opening.cs and OpeningGenerator.cs with basic door placement
2. **Day 2**: Implement window distribution algorithm and wall analysis
3. **Day 3**: Integrate opening generation into GeometryGeneratorService
4. **Day 4**: Implement wall cutting and frame generation
5. **Day 5**: Add glass and door materials to frontend
6. **Day 6**: Create unit tests and fix bugs
7. **Day 7**: Visual testing with all 15 layout combinations
8. **Day 8**: Add style variations and polish
9. **Day 9**: Database migration and API updates
10. **Day 10**: Frontend UI controls and integration testing

## Success Criteria

- [x] All existing designs still render correctly (regression test)
- [ ] Main door appears on front wall of all buildings
- [ ] Windows distributed on all exterior walls
- [ ] Multi-story buildings have windows on each floor
- [ ] Glass material is transparent and realistic
- [ ] Door and window frames are visible
- [ ] No geometry artifacts or z-fighting
- [ ] Performance remains acceptable (< 2 second render time)
- [ ] All unit tests pass
- [ ] Visual inspection of all 15 test cases shows proper openings
- [ ] Different architectural styles produce visually distinct results
- [ ] Camera controls allow detailed inspection of openings

## Known Challenges and Solutions

### Challenge 1: Wall Intersection Detection
**Problem**: Determining exactly where opening intersects wall mesh
**Solution**: Use wall plane equation and opening bounding box for intersection tests

### Challenge 2: Interior Wall Identification
**Problem**: Distinguishing interior walls from exterior walls
**Solution**: Track wall type during generation in LayoutStrategy classes

### Challenge 3: Window Alignment Across Floors
**Problem**: Upper floor windows should align with lower floors for aesthetic appeal
**Solution**: Store ground floor window X-positions and reuse for upper floors

### Challenge 4: Angled Wall Windows
**Problem**: Windows on non-axis-aligned walls need proper rotation
**Solution**: Use wall normal vector to rotate window geometry

### Challenge 5: Glass Rendering Performance
**Problem**: Transparent materials can slow rendering
**Solution**: Use simplified glass material, limit transparency effects

## Future Enhancements (Post-Initial Implementation)

1. **Balconies**: Exterior platforms with railings for upper floors
2. **Bay Windows**: Protruding window structures
3. **Garage Doors**: Large doors for attached garages
4. **French Doors**: Glass door pairs
5. **Skylights**: Roof-mounted windows
6. **Window Shutters**: Decorative exterior elements
7. **Door Handles**: Detailed 3D hardware
8. **Window Boxes**: Decorative flower boxes
9. **Custom Placement**: User-specified window/door positions
10. **Energy Ratings**: Calculate natural light and ventilation

## Reference Materials

- **Three.js Glass Tutorial**: Transmission material properties
- **Architectural Standards**: Standard door/window dimensions
- **Building Codes**: Minimum window area requirements (typically 8-10% floor area)
- **Style Guides**: Window patterns for different architectural styles

## Notes

- Keep door/window geometry simple initially - focus on correct placement
- Frame detail can be added incrementally
- Test with dark theme camera controls for detailed inspection
- Consider adding "Show Openings" toggle for debugging
- Document window spacing algorithm for future adjustments
- Ensure opening geometry exports correctly to .obj files

---

**Start Date**: After all 15 TESTING_PLAN.md cases pass
**Estimated Duration**: 10 days
**Priority**: HIGH (core architectural feature)
**Dependencies**: Completed geometry testing, working camera controls
