# Testing & Debug Plan - All Layouts and Story Combinations

**Date:** November 28, 2025  
**Purpose:** Systematic testing of all 5 layouts × 3 story counts to identify and fix rendering issues

---

## Testing Matrix

### Layout Types (Determined by Lot Size % 5)

| Layout # | Type | Lot Size Examples | Description |
|----------|------|-------------------|-------------|
| 0 | Traditional Cube | 2500, 2505, 2510 | Single rectangular building |
| 1 | Two-Story | 2501, 2506, 2511 | Upper floor 85% of lower |
| 2 | L-Shape | 2502, 2507, 2512 | Main wing + side wing |
| 3 | Split-Level | 2503, 2508, 2513 | Two offset sections |
| 4 | Angled Modern | 2504, 2509, 2514 | Rotated sections |

### Story Counts to Test
- **1 Story:** Baseline, simplest case
- **2 Stories:** Common residential
- **3 Stories:** Complex multi-level

---

## Test Cases (15 Total)

### Modern Style (Flat Roof)

#### Test 1: Modern, Cube, 1-Story
```
Parameters:
Lot Size: 2500
Style: "modern minimalist"
Expected: Single rectangular building, flat roof, large windows
```

**Results:**
- [ ] Building renders correctly
- [ ] Roof positioned on top
- [ ] Correct number of floors (1)
- [ ] Interior walls present
- [ ] No geometry issues

**Issues Found:**
```
(Fill in any issues encountered during testing)

```

**Screenshots:** `test_01_modern_cube_1story.png`

---

#### Test 2: Modern, Cube, 2-Story
```
Parameters:
Lot Size: 2500
Style: "modern minimalist"
Stories Override: 2
Expected: Two floors, flat roof on top, windows on both levels
```

**Results:**
- [ ] Building renders correctly
- [ ] Roof positioned on top
- [ ] Correct number of floors (2)
- [ ] Interior walls present on both floors
- [ ] No geometry issues

**Issues Found:**
```
(Fill in any issues encountered during testing)

```

**Screenshots:** `test_02_modern_cube_2story.png`

---

#### Test 3: Modern, Cube, 3-Story
```
Parameters:
Lot Size: 2500
Style: "modern minimalist"
Stories Override: 3
Expected: Three floors, flat roof, windows on all levels
```

**Results:**
- [ ] Building renders correctly
- [ ] Roof positioned on top
- [ ] Correct number of floors (3)
- [ ] Interior walls present on all floors
- [ ] No geometry issues

**Issues Found:**
```
(Fill in any issues encountered during testing)

```

**Screenshots:** `test_03_modern_cube_3story.png`

---

#### Test 4: Modern, Two-Story, 1-Story
```
Parameters:
Lot Size: 2501
Style: "modern glass house"
Expected: Single level despite "two-story" layout type
```

**Results:**
- [ ] Building renders correctly
- [ ] Roof positioned on top
- [ ] Correct number of floors (1)
- [ ] Interior walls present
- [ ] No geometry issues

**Issues Found:**
```
(Fill in any issues encountered during testing)

```

**Screenshots:** `test_04_modern_twostory_1story.png`

---

#### Test 5: Modern, Two-Story, 2-Story
```
Parameters:
Lot Size: 2501
Style: "modern glass house"
Stories Override: 2
Expected: Ground floor + smaller upper floor (85% scale)
```

**Results:**
- [ ] Building renders correctly
- [ ] Roof positioned on top
- [ ] Correct number of floors (2)
- [ ] Upper floor scaled correctly (85%)
- [ ] Interior walls present on both floors
- [ ] No geometry issues

**Issues Found:**
```
(Fill in any issues encountered during testing)

```

**Screenshots:** `test_05_modern_twostory_2story.png`

---

#### Test 6: Modern, Two-Story, 3-Story
```
Parameters:
Lot Size: 2501
Style: "modern glass house"
Stories Override: 3
Expected: Three progressively smaller floors OR stacked floors with scaled top
```

**Results:**
- [ ] Building renders correctly
- [ ] Roof positioned on top
- [ ] Correct number of floors (3)
- [ ] Floor scaling appropriate
- [ ] Interior walls present on all floors
- [ ] No geometry issues

**Issues Found:**
```
(Fill in any issues encountered during testing)

```

**Screenshots:** `test_06_modern_twostory_3story.png`

---

#### Test 7: Modern, L-Shape, 1-Story
```
Parameters:
Lot Size: 2502
Style: "modern"
Expected: Two perpendicular wings, single story, two flat roof sections
```

**Results:**
- [ ] Building renders correctly
- [ ] Both wings visible and perpendicular
- [ ] Roofs positioned on top of both wings
- [ ] Correct number of floors (1)
- [ ] Interior walls present
- [ ] No geometry issues

**Issues Found:**
```
(Fill in any issues encountered during testing)

```

**Screenshots:** `test_07_modern_lshape_1story.png`

---

#### Test 8: Modern, L-Shape, 2-Story
```
Parameters:
Lot Size: 2502
Style: "modern"
Stories Override: 2
Expected: L-shaped footprint, two stories tall, roof on top of both wings
```

**Results:**
- [ ] Building renders correctly
- [ ] Both wings visible and perpendicular
- [ ] Roofs positioned on top of both wings
- [ ] Correct number of floors (2)
- [ ] Interior walls present on both floors
- [ ] No geometry issues

**Issues Found:**
```
(Fill in any issues encountered during testing)

```

**Screenshots:** `test_08_modern_lshape_2story.png`

---

#### Test 9: Modern, L-Shape, 3-Story
```
Parameters:
Lot Size: 2502
Style: "modern"
Stories Override: 3
Expected: L-shaped footprint, three stories tall
```

**Results:**
- [ ] Building renders correctly
- [ ] Both wings visible and perpendicular
- [ ] Roofs positioned on top of both wings
- [ ] Correct number of floors (3)
- [ ] Interior walls present on all floors
- [ ] No geometry issues

**Issues Found:**
```
(Fill in any issues encountered during testing)

```

**Screenshots:** `test_09_modern_lshape_3story.png`

---

#### Test 10: Modern, Split-Level, 1-Story
```
Parameters:
Lot Size: 2503
Style: "modern contemporary"
Expected: Two sections at different heights, single level each
```

**Results:**
- [ ] Building renders correctly
- [ ] Two sections visible at different heights
- [ ] Roofs positioned on top of both sections
- [ ] Split-level effect visible
- [ ] Interior walls present
- [ ] No geometry issues

**Issues Found:**
```
(Fill in any issues encountered during testing)

```

**Screenshots:** `test_10_modern_splitlevel_1story.png`

---

#### Test 11: Modern, Split-Level, 2-Story
```
Parameters:
Lot Size: 2503
Style: "modern contemporary"
Stories Override: 2
Expected: Split-level with multiple floor levels
```

**Results:**
- [ ] Building renders correctly
- [ ] Multiple sections with varying heights
- [ ] Roofs positioned correctly on all sections
- [ ] Correct number of floors
- [ ] Interior walls present on all levels
- [ ] No geometry issues

**Issues Found:**
```
(Fill in any issues encountered during testing)

```

**Screenshots:** `test_11_modern_splitlevel_2story.png`

---

#### Test 12: Modern, Angled, 1-Story
```
Parameters:
Lot Size: 2504
Style: "modern"
Expected: Rotated sections (22.5° and -30°), single story
```

**Results:**
- [ ] Building renders correctly
- [ ] Sections rotated at correct angles
- [ ] Roofs positioned on top of all sections
- [ ] Correct number of floors (1)
- [ ] Interior walls present
- [ ] No geometry issues

**Issues Found:**
```
(Fill in any issues encountered during testing)

```

**Screenshots:** `test_12_modern_angled_1story.png`

---

#### Test 13: Modern, Angled, 2-Story
```
Parameters:
Lot Size: 2504
Style: "modern"
Stories Override: 2
Expected: Rotated sections, two stories tall
```

**Results:**
- [ ] Building renders correctly
- [ ] Sections rotated at correct angles
- [ ] Roofs positioned on top of all sections
- [ ] Correct number of floors (2)
- [ ] Interior walls present on both floors
- [ ] No geometry issues

**Issues Found:**
```
(Fill in any issues encountered during testing)

```

**Screenshots:** `test_13_modern_angled_2story.png`

---

#### Test 14: Modern, Angled, 3-Story
```
Parameters:
Lot Size: 2504
Style: "modern"
Stories Override: 3
Expected: Rotated sections, three stories tall
```

**Results:**
- [ ] Building renders correctly
- [ ] Sections rotated at correct angles
- [ ] Roofs positioned on top of all sections
- [ ] Correct number of floors (3)
- [ ] Interior walls present on all floors
- [ ] No geometry issues

**Issues Found:**
```
(Fill in any issues encountered during testing)

```

**Screenshots:** `test_14_modern_angled_3story.png`

---

### Victorian Style (Gabled Roof)

#### Test 15: Victorian, L-Shape, 2-Story
```
Parameters:
Lot Size: 2502
Style: "victorian ornate"
Expected: L-shaped, gabled roofs on both wings, two stories, ornate windows
```

**Results:**
- [ ] Building renders correctly
- [ ] Both wings visible and perpendicular
- [ ] Gabled roofs positioned correctly on both wings
- [ ] Correct number of floors (2)
- [ ] Interior walls present on both floors
- [ ] Victorian style elements visible
- [ ] No geometry issues

**Issues Found:**
```
(Fill in any issues encountered during testing)

```

**Screenshots:** `test_15_victorian_lshape_2story.png`

---

## Testing Summary

**Tests Completed:** 0 / 15

**Tests Passing:** 0

**Tests Failing:** 0

**Common Issues Identified:**
```
(After testing, summarize recurring patterns across multiple tests)


```

**Priority Fixes:**
1. 
2. 
3. 

---

## Known Issues to Check

### 1. Roof Positioning
- **Issue:** Roof may be at ground level instead of on top of building
- **Check:** Roof Y position = building top + (roof height / 2)
- **Files:** `FlatRoofStrategy.cs`, `GabledRoofStrategy.cs`

### 2. Story Override Missing Interior Walls
- **Issue:** Upper floors may lack interior walls when stories overridden
- **Check:** Backend generates rooms for all floors
- **Files:** `DesignsController.cs` room generation, frontend interior wall renderer

### 3. Layout Selection Not Respected
- **Issue:** User selects "cube" but gets "l-shape"
- **Check:** Backend uses buildingShapeOverride before geometry generation
- **Files:** `DesignsController.cs`, `MainScreen.js`

### 4. Geometry Not Rendering
- **Issue:** Only green ground plane, no building
- **Check:** Backend returns geometry, frontend passes to HouseViewer3D
- **Files:** `MainScreen.js`, `HouseViewer3D.js`, `GeometryRenderer.js`

### 5. Multiple Roof Sections (L-Shape, Split-Level)
- **Issue:** Roofs may overlap or be misaligned on multi-section layouts
- **Check:** Each section gets correctly positioned roof
- **Files:** `RoofService.cs`, `LayoutService.cs`

---

## Testing Procedure

### Step 1: Visual Inspection Checklist

For each test case, verify:

- [ ] **Building renders** (not just green ground)
- [ ] **Correct number of floors** (matches story count)
- [ ] **Roof is on top** (not at ground level)
- [ ] **Roof type correct** (flat for Modern, gabled for Victorian)
- [ ] **Windows present** on all visible walls
- [ ] **Door visible** at ground level
- [ ] **Layout matches** expected shape (cube, L, angled, etc.)
- [ ] **Interior walls present** on all floors (if applicable)
- [ ] **No geometry clipping** (walls don't intersect incorrectly)
- [ ] **Proper scaling** (building looks proportional)

### Step 2: Console Log Review

Check for errors or warnings:
```javascript
// Frontend console
✅ Backend geometry received: {sections: X, roofs: Y}
🔨 Creating mesh: {vertexCount: X, faceCount: Y}
❌ Any errors about invalid geometry
```

```csharp
// Backend logs
INFO: Generating layout: cube, stories: 2
INFO: Creating X sections
INFO: Generating Y roofs
```

### Step 3: Data Inspection

For each test, log key values:
```javascript
// In browser console after generation
console.log('Sections:', houseParams.geometry.sections.length);
console.log('Roofs:', houseParams.geometry.roofs.length);
console.log('Total Height:', houseParams.geometry.totalHeight);
console.log('Stories:', houseParams.stories);
```

### Step 4: Screenshot Capture

Take screenshots of each test case for comparison:
- Filename format: `test_XX_style_layout_stories.png`
- Example: `test_02_modern_cube_2story.png`

---

## Expected Behavior by Layout

### Layout 0: Cube
- **Sections:** 1
- **Roofs:** 1
- **Geometry:** Simple rectangular box
- **Height:** ceilingHeight × stories

### Layout 1: Two-Story
- **Sections:** 2 (ground floor + upper floor)
- **Roofs:** 1 (on upper floor)
- **Geometry:** Upper floor 85% size of lower
- **Height:** ceilingHeight × 2 (even if 1-story selected)

### Layout 2: L-Shape
- **Sections:** 2 (main wing + side wing)
- **Roofs:** 2 (one per wing)
- **Geometry:** Perpendicular wings
- **Height:** ceilingHeight × stories (both wings same height)

### Layout 3: Split-Level
- **Sections:** 2 (lower + upper offset)
- **Roofs:** 2 (one per section)
- **Geometry:** Sections at different Y positions
- **Height:** Varies per section

### Layout 4: Angled
- **Sections:** 2-3 (base + rotated sections)
- **Roofs:** 2-3 (one per section)
- **Geometry:** Sections rotated 22.5° and -30°
- **Height:** ceilingHeight × stories

---

## Debug Workflow

### When a Test Fails:

1. **Identify the issue category:**
   - Geometry not rendering → Check backend response, frontend geometry passing
   - Roof mispositioned → Check roof Y calculation
   - Wrong layout → Check layout seed calculation, backend override
   - Missing interior walls → Check room generation for story count
   - Geometry clipping → Check section positioning and dimensions

2. **Backend debugging:**
   ```bash
   # Start backend with verbose logging
   cd ArchitecturalDreamMachineBackend/ArchitecturalDreamMachineBackend
   dotnet run --verbosity detailed
   ```
   
   Check logs for:
   - Layout type selected
   - Number of sections created
   - Roof calculations
   - Section positions and dimensions

3. **Frontend debugging:**
   ```javascript
   // Add to HouseViewer3D.js or GeometryRenderer.js
   console.log('🔍 GEOMETRY DEBUG:', {
     sectionsCount: buildingGeometry.sections.length,
     roofsCount: buildingGeometry.roofs.length,
     totalHeight: buildingGeometry.totalHeight,
     firstSection: buildingGeometry.sections[0]
   });
   ```

4. **API testing:**
   ```bash
   # Test backend directly with curl
   curl -X POST http://localhost:5095/api/designs/generate \
     -H "Content-Type: application/json" \
     -d '{
       "lotSize": 2502,
       "stylePrompt": "modern",
       "buildingShapeOverride": "l-shape",
       "storiesOverride": 2
     }' | jq .
   ```

5. **Compare with working case:**
   - Find a similar test that works
   - Compare geometry data structures
   - Identify differences

---

## Common Fix Patterns

### Roof at Ground Level
```csharp
// FlatRoofStrategy.cs
// WRONG:
Y = section.Y

// RIGHT:
Y = section.Y + section.Height + (roofThickness / 2)
```

### Missing Interior Walls on Upper Floors
```csharp
// DesignsController.cs
// Ensure room generation duplicates for all floors
if (stories > 1)
{
    var firstFloorRooms = rooms.Where(r => r.Floor == 1).ToList();
    for (int floor = 2; floor <= stories; floor++)
    {
        rooms.AddRange(DuplicateFloorLayout(firstFloorRooms, floor));
    }
}
```

### Layout Override Not Working
```csharp
// DesignsController.cs - GenerateRequest
// Ensure overrides applied BEFORE layout calculation
var shape = request.BuildingShapeOverride ?? styleTemplate.BuildingShape;
var stories = request.StoriesOverride ?? styleTemplate.TypicalStories;
```

### Geometry Not Rendering
```javascript
// MainScreen.js
// Ensure geometry passed to component
if (response.data.geometry) {
  params.geometry = response.data.geometry;
}

// HouseViewer3D.js
// Check geometry exists before rendering
if (houseParams.geometry) {
  GeometryRenderer.renderBuilding(houseGroup, houseParams.geometry);
}
```

---

## Success Criteria

All 15 tests pass with:
- ✅ Correct layout shape rendered
- ✅ Correct number of stories
- ✅ Roof positioned on top of building
- ✅ Interior walls on all floors (when stories > 1)
- ✅ No console errors
- ✅ Geometry matches backend specifications
- ✅ Visual appearance is architecturally sound

---

## Testing Tools

### Backend
- Swagger UI: http://localhost:5095/swagger
- Direct API testing with curl
- Backend logs (console output)

### Frontend
- Browser DevTools Console
- React DevTools (component inspection)
- Three.js Inspector: https://threejs.org/editor/

### Comparison
- Screenshot diff tools
- Side-by-side browser windows

---

## Next Steps After Testing

1. **Document all failures** in a spreadsheet
2. **Prioritize fixes** (critical rendering issues first)
3. **Create GitHub issues** for each bug category
4. **Fix systematically** (one layout at a time)
5. **Regression test** (ensure fixes don't break other layouts)
6. **Update tests** (add automated tests for fixed issues)

---

**Ready to start testing tomorrow morning!** 🚀

Run tests systematically, document findings, fix issues one by one.
