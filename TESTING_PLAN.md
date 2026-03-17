# Testing Plan — All Layouts x Stories

**Created:** November 28, 2025
**Updated:** March 17, 2026
**Purpose:** Systematic testing of 5 layouts x 3 story counts to verify rendering.
**Status:** Ready to execute — Wall Face Fix complete, geometry pipeline stable

---

## Test Matrix

### Layouts (determined by `lotSize % 5` or `buildingShapeOverride`)

| # | Type | Lot Size | Sections | Roofs |
|---|------|----------|----------|-------|
| 0 | Cube | 2500 | 1 | 1 |
| 1 | Two-Story | 2501 | 1 (multi-floor) | 1 (on top) |
| 2 | L-Shape | 2502 | 2 (main + corner wing) | 2 |
| 3 | Split-Level | 2503 | 2 (main 2-story right + wing 1-story left) | 2 |
| 4 | Angled | 2504 | 2 (tower center + wing offset front-right) | 2 |

### Stories: 1, 2, 3 (use `storiesOverride`)

---

## Test Cases (16 Total)

All use Modern style (flat roof) unless noted. Use Swagger or curl to test directly.

| # | Layout | Stories | Lot Size | Style | Notes |
|---|--------|---------|----------|-------|-------|
| 1 | Cube | 1 | 2500 | modern | Baseline |
| 2 | Cube | 2 | 2500 | modern | Windows on both levels |
| 3 | Cube | 3 | 2500 | modern | Windows on all levels |
| 4 | Two-Story | 1 | 2501 | modern | Single level despite layout type |
| 5 | Two-Story | 2 | 2501 | modern | Upper floor 85% scale |
| 6 | Two-Story | 3 | 2501 | modern | Three progressively smaller floors |
| 7 | L-Shape | 1 | 2502 | modern | Two perpendicular wings |
| 8 | L-Shape | 2 | 2502 | modern | Both wings two stories |
| 9 | L-Shape | 3 | 2502 | modern | Both wings three stories |
| 10 | Split-Level | 1 | 2503 | modern | Two sections at different heights |
| 11 | Split-Level | 2 | 2503 | modern | Multi-level offset |
| 12 | Split-Level | 3 | 2503 | modern | ⭐ Primary regression test for wall face fix |
| 13 | Angled | 1 | 2504 | modern | Rotated sections |
| 14 | Angled | 2 | 2504 | modern | Rotated, two stories |
| 15 | Angled | 3 | 2504 | modern | Rotated, three stories |
| 16 | L-Shape | 2 | 2502 | victorian | Gabled roofs on both wings |

**Tests completed:** 0 / 16

---

## Per-Test Checklist

For each test, verify:

- [ ] Building renders (not just green ground)
- [ ] Correct number of floors
- [ ] Roof on top (not at ground level), correct type (flat/gabled)
- [ ] Layout matches expected shape
- [ ] Windows present on all exterior walls
- [ ] Interior walls present on all floors
- [ ] No geometry clipping or artifacts
- [ ] No console errors (frontend or backend)

---

## Known Issues to Watch For

**~~Floating windows on multi-section layouts~~** — ✅ Fixed by `WallFaceService.ComputeOverlapHoles()` and window filtering.

**~~Wall panels sticking through sections~~** — ✅ Fixed by overlap hole computation in `WallFaceService`.

**~~Parapet rotation/positioning~~** — ✅ Fixed: Left/right parapets rotated, placed on roof surface, fence-style.

**~~Split-level interior walls outside building~~** — ✅ Fixed: `GenerateSplitLevelRoomLayout()` constrains floor 2 to main section.

**~~Angled wing missing windows~~** — ✅ Fixed: Synthetic wing room + section-aware window generation.

**Roof at ground level** — Check roof Y calculation: should be `section.Y + section.Height + (roofThickness / 2)`. Files: `FlatRoofStrategy.cs`, `GabledRoofStrategy.cs`.

**Missing interior walls on upper floors** — Backend must generate rooms for all floors, not just ground. Files: `DesignOrchestrationService.cs`.

**L-Shape void quadrant** — Rooms/windows should not appear in the front-right void area. Files: `LShapeLayoutStrategy.cs`, `WindowService.cs`.

**Multi-section roof overlap** — Each section needs its own correctly positioned roof. Files: `RoofService.cs`, `LayoutService.cs`.

---

## Debug Workflow

1. Test backend directly:
```bash
curl -X POST http://localhost:5095/api/designs/generate \
  -H "Content-Type: application/json" \
  -d '{"lotSize": 2502, "stylePrompt": "modern", "buildingShapeOverride": "l-shape", "storiesOverride": 2}'
```

2. Check backend logs for layout type, section count, roof calculations.

3. In browser console, inspect geometry data:
```javascript
console.log('Sections:', houseParams.geometry.sections.length);
console.log('Roofs:', houseParams.geometry.roofs.length);
console.log('Total Height:', houseParams.geometry.totalHeight);
```
