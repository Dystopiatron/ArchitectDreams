# Fix: Floating Windows + Exposed Wall Panels on Multi-Section Layouts

**Date:** 2026-02-20
**Branch:** `FT/OverClock`
**Status:** ✅ Implemented (completed 2026-03)

---

## Problem Summary

When generating multi-section buildings (split-level, angled modern, L-shape) with 3+ stories,
the 3D model shows:

1. **White wall panels sticking up** — Face panels from one section render at full height even
   where a shorter/overlapping section covers part of them
2. **Floating window glass quads** — Blue/green glass rectangles floating in mid-air with no wall
   behind them
3. **Z-fighting flicker** — On split-level specifically, both sections share the same front/back
   Z position, causing two face panels to overlap and flicker

The user reported this on the **split-level "Modern Minimalist" 3-story** configuration.
Screenshots: `modernMinimalistThreeStory.jpeg` and `modernMinimalistThreeStoryOtherSide.jpeg`.

---

## Root Cause Analysis

### Current Architecture

The wall-cutting geometry system (recently implemented) works like this:

1. **Backend `WallFaceService`** generates one `WallFaceData` panel per face per building section
   (4 faces × N sections). Each panel carries `WallOpeningData` entries for windows/doors.
2. **Backend `IsFaceInterior()`** suppresses faces that are overwhelmingly inside another section
   (≥50% height overlap AND plane strictly inside AND ≥35% span overlap).
3. **Frontend `GeometryRenderer.js`** renders face panels as `THREE.ShapeGeometry` with holes
   punched for each opening. Window glass quads are rendered as **separate independent meshes**
   from `buildingGeometry.windows`.

### What Goes Wrong

**Issue 1: No partial overlap handling.**
`IsFaceInterior()` is all-or-nothing — it either suppresses the entire face or keeps it.
When a face is only *partially* behind another section (e.g., the lower 1ft of a 5ft face),
the full face renders. For split-level: the upper section's LEFT face at X=-0.1W is inside
the lower section's X range, but only 1ft of 5ft overlaps in height (20% < 50% threshold) →
not suppressed → full panel renders as a wall sticking out.

**Issue 2: Window glass quads not filtered.**
`buildingGeometry.Windows` (the `GeometryData` list) includes ALL windows that passed the
XZ-bounds filter (Step 5c in orchestration). When `IsFaceInterior()` suppresses a face,
the window glass quads on that face are NOT removed. They float in mid-air.

**Issue 3: Coplanar face panels.**
Split-level sections share the same Z depth/position. Both sections get front face panels at
the same Z coordinate. In the overlapping X/Y range, two panels exist at the same position →
z-fighting.

**Issue 4: Floor 3+ windows with no section.**
`SplitLevelLayoutStrategy` always creates exactly 2 sections (floor 1 + floor 2) regardless
of the `stories` parameter. When stories=3, rooms/windows are generated for 3 floors, but
floor 3 has no section. Those windows pass the XZ filter but never match a face panel → float.

---

## Split-Level Layout Specifics

For `ceilingHeight=10ft`, `footprintWidth=W`, `footprintDepth=D`, `stories=3`:

| Section | X | Y_center | Z | Width | Height | Depth | Floor | Base Y | Top Y |
|---------|---|----------|---|-------|--------|-------|-------|--------|-------|
| Lower | 0 | 3.5 | 0 | W | 7ft | 0.7D | 1 | 0 | 7 |
| Upper | 0.2W | 8.5 | 0 | 0.6W | 5ft | 0.7D | 2 | 6 | 11 |

Both sections have **identical Z range** (same center Z=0, same depth=0.7D).
Upper section is **narrower** (60% width) and **offset** in X by 0.2W.
Upper section X range: -0.1W to 0.5W (fully within lower's -0.5W to 0.5W).

---

## Planned Fix

### A. Section Overlap Holes (fixes issues 1, 3)

Add a new method `ComputeOverlapHoles()` to `WallFaceService`. For each face panel being built,
check if any other section covers part of the face from outside. If so, add rectangular
`WallOpeningData` entries (type="overlap") that punch holes in the face panel where it's hidden.

**The frontend already handles this with zero changes** — the `createWallFacePanel()` method in
`GeometryRenderer.js` punches holes for ALL openings regardless of type.

**Algorithm:**

```
For each face (section, direction, baseY):
  For each other section:
    1. Does other section REACH this face plane from outside?
       - Front face at Z_face: other reaches if:
         other.Z + other.Depth/2 >= Z_face AND other.Z - other.Depth/2 < Z_face
       - Track whether it's "strictly past" (>) or "coplanar" (==)
       - Same logic for Back, Right, Left with appropriate axes

    2. Compute span overlap (X range for Front/Back, Z range for Right/Left)
       overlapMin = max(faceSpanMin, otherSpanMin)
       overlapMax = min(faceSpanMax, otherSpanMax)
       If overlapMax <= overlapMin: skip

    3. Compute height overlap
       hMin = max(baseY, otherBaseY)
       hMax = min(topY, otherTopY)
       If hMax <= hMin: skip

    4. COPLANAR TIEBREAKER (when face planes are equal, not strictly past):
       Only add hole if the other section extends HIGHER (otherTopY > topY).
       This gives priority to the taller section's face panel.
       If same topY: higher baseY wins. If identical: skip (arbitrary tiebreaker).

    5. Convert overlap rectangle to face-local 2D coords:
       Front:  offsetX = overlapCenterX - sec.X,      offsetY = overlapCenterY - baseY
       Back:   offsetX = sec.X - overlapCenterX,       offsetY = overlapCenterY - baseY
       Right:  offsetX = overlapCenterZ - sec.Z,        offsetY = overlapCenterY - baseY
       Left:   offsetX = sec.Z - overlapCenterZ,        offsetY = overlapCenterY - baseY

    6. Add margin of ~0.1ft to prevent z-fighting at overlap edges
```

**Expected overlap zones for split-level:**
- Upper LEFT face (X=-0.1W): lower extends past in -X → hole at Y=6-7ft, full depth span
- Lower FRONT face (Z=0.35D): upper is coplanar, upper topY=11 > lower topY=7 → hole at
  X=[-0.1W, 0.5W], Y=[6, 7]
- Lower BACK face: same coplanar logic → matching hole
- Lower RIGHT face (X=0.5W): upper RIGHT edge also at 0.5W — coplanar, upper wins → hole

### B. Window Filtering (fixes issues 2, 4)

Change `WallFaceService` to track which `WindowElement.Id`s were successfully placed on rendered
face panels (not suppressed by IsFaceInterior AND not inside an overlap zone).

Return a `WallFaceResult` containing both the face list and the placed window IDs.

In `DesignOrchestrationService`, after Step 5d, filter the `windows` GeometryData list and
`windowElements` list to only include placed windows.

---

## Files to Modify

### 1. `Geometry/WallFaceData.cs`
Add new class:
```csharp
public class WallFaceResult
{
    public List<WallFaceData> Faces { get; set; } = new();
    public HashSet<string> PlacedWindowIds { get; set; } = new();
}
```

### 2. `Services/IWallFaceService.cs`
Change return type:
```csharp
WallFaceResult GenerateWallFaces(
    List<LayoutSection> sections,
    List<WindowElement>  windows,
    List<DoorElement>    doors,
    string materialType,
    string color);
```

### 3. `Services/WallFaceService.cs`
This is the bulk of the work:

- **`GenerateWallFaces()`** — returns `WallFaceResult`. Passes `allSections` to `BuildFace`.
  Collects `placedWindowIds` across all calls.

- **`BuildFace()`** — gains `List<LayoutSection> allSections` and
  `HashSet<string> placedWindowIds` parameters. After creating the face:
  1. Calls `ComputeOverlapHoles()` and adds results to `face.Openings`
  2. When iterating windows, checks if each window center falls inside any overlap hole.
     If inside → skip (don't add to face openings or placedWindowIds).
     If outside → add to face openings AND `placedWindowIds.Add(w.Id)`.

- **New `ComputeOverlapHoles()`** — implements the algorithm above.

- **`WindowInOverlapZone()`** — helper to check if a window's face-local position falls inside
  any overlap hole rectangle.

### 4. `Services/DesignOrchestrationService.cs`
After Step 5d, add Step 5e:
```csharp
// Step 5e: Filter window geometry to only include windows on rendered face panels.
var placedIds = wallFaceResult.PlacedWindowIds;
windowElements = windowElements.Where(we => placedIds.Contains(we.Id)).ToList();
windows = windows.Where(w =>
    windowElements.Any(we =>
        Math.Abs((w.Position?.X ?? 0) - we.X) < 0.15 &&
        Math.Abs((w.Position?.Y ?? 0) - we.Y) < 0.15 &&
        Math.Abs((w.Position?.Z ?? 0) - we.Z) < 0.15)
).ToList();
```

### 5. No Frontend Changes
`GeometryRenderer.js` already handles any opening type — overlap holes will be punched
identically to window/door holes. Filtered windows simply won't appear in the response.

---

## Key Coordinate System Reference

All measurements in **feet**. Y-up coordinate system.

- `LayoutSection.Y` = **center Y** of box (not base). Base Y = `sec.Y - sec.Height / 2`
- Face panel offset: 0.02ft outward (SkinOffset) to prevent z-fighting with section box
- `WindowElement` positions: X, Y, Z in world coordinates. Y = center of window.
- Face-local coords: OffsetX from face center, OffsetY from section base Y
- Front/Back faces span the X axis; Right/Left faces span the Z axis
- RotationY: Front=0, Back=π, Right=-π/2, Left=π/2

**OffsetX mirroring:** Back and Left faces are rotated (π and π/2), so their local +X runs
opposite to world coordinates. The offset formulas invert accordingly.

---

## Verification

```bash
cd ArchitecturalDreamMachineBackend/ArchitecturalDreamMachineBackend
dotnet build    # Expect 0 errors
dotnet test     # Expect 10 pass (existing tests shouldn't break)
```

### Visual Tests
Split-level 3-story (primary target):
- [ ] No floating window glass quads
- [ ] No white panels sticking through other sections
- [ ] No z-fighting on shared front/back faces
- [ ] Upper section walls visible above lower section
- [ ] Windows with holes only on exposed face areas

Regression checks:
- [ ] Rectangular (single section) — unaffected
- [ ] L-shape — step faces preserved
- [ ] Angled modern — previous fix still works, overlap holes improve tower faces

---

## Context for Next Agent

- The wall-cutting geometry system was just implemented (commit `c4b1e8d` on `FT/OverClock`)
- `IsFaceInterior()` already handles full-face suppression (≥50% height, ≥35% span thresholds)
- This fix adds **partial overlap handling** on top of the existing suppression
- The `IsFaceInterior` check can stay as-is — it's a fast-path for faces that are overwhelmingly
  interior. The overlap holes handle the remaining partial cases.
- Window glass quads are currently rendered as independent meshes in the frontend
  (`buildingGeometry.windows` array). They're separate from the face panel hole punching.
  Both exist: the hole shows the interior, the glass quad fills it with translucent material.
