# Roadmap

Completed work and planned enhancements.

---

## Completed

### Code Quality & Architecture
- Interface extraction for all core services (IDesignOrchestrationService, IGeometryService, ILayoutService, IRoofService, etc.)
- Magic numbers extracted to `Constants/ArchitecturalConstants.cs`
- HouseParametersService extracted from controller
- WindowService and InteriorWallService replace TODO placeholders
- Consistent null handling in HouseParameters

### API & Validation
- Typed DTOs (`GenerateResponse`, `ErrorResponse`, `DesignSummary`)
- FluentValidation on requests
- Input sanitization in PromptParser
- Centralized API URL config (`config/api.js`)
- CrossPlatformPicker for React Native compatibility

### Security (Phase 1 & 2)
- CORS scoped to configured origins (AllowAnyOrigin only in Development)
- HSTS in non-development environments
- Rate limiting on POST endpoints
- API key authentication (`X-API-Key` header)
- Pinned NuGet + npm dependencies
- Geometry input bounds validation

### Exports
- OBJ mesh export
- IFC4 via xBIM: full IfcProject hierarchy, IfcWallStandardCase, IfcWindow with openings, IfcDoor, IfcSlab, IfcRoof
- GLB via SharpGLTF: PBR materials, transparent glass, style-specific colors

### 3D Viewer
- Interactive camera (zoom, rotate, preset views)
- Dark theme UI
- Auto-rotate with `autoRotateRef` (stale closure fix)

### Semantic Model (Phase 3)
- WallSegment entity with start/end coordinates, type, openings
- BuildingModel aggregate (floors, walls, doors, windows, slabs, roof)
- Services updated to produce semantic entities alongside GeometryData

### Wall Face System (Phase 4) — Completed 2026-03
- `WallFaceService` generates perforated wall panels with window/door cutouts
- `ComputeOverlapHoles()` punches holes where sections overlap (fixes floating panels)
- `WallFaceResult` tracks `PlacedWindowIds` to filter floating window geometry
- Coplanar face tiebreaker gives priority to taller sections
- Frontend `createWallFacePanel()` renders with `THREE.ShapeGeometry` holes
- Fixes: floating windows, wall panels sticking through sections, z-fighting

### Style System Improvements (2026-03)
- `MaxWindowsPerRoom` raised from 5 to 12
- `WindowStyle` now affects geometry: small=2x3ft, large=5x6ft, ornate=3x5ft
- `RoofGeometry.Parapets` collection — parapets render in GLB and frontend
- `StyleResolverService` consolidates style matching (was duplicated 4×)

---

## Pending

### Style System Audit (2026-02-21) — Updated 2026-03-17

Comprehensive review of style differentiation (Modern, Victorian, Brutalist).

**No breaking conflicts found** — styles don't clash during generation.

#### Fixed Issues ✅

| Issue | Resolution |
|-------|------------|
| `MaxWindowsPerRoom=5` clamps all styles | ✅ Raised to 12 in `ArchitecturalConstants.cs` |
| WindowStyle decorative only | ✅ `WindowService.GetWindowDimensions()` now maps small=2x3, large=5x6, ornate=3x5 |
| Parapets generated but discarded | ✅ `RoofGeometry.Parapets` collection added; GLB/frontend render them |
| Style matching duplicated 4× | ✅ `StyleResolverService` consolidates all style resolution |
| SplitLevelLayoutStrategy ignores stories > 2 | ✅ Never true — code correctly loops all floors |

#### Remaining Issues

| Issue | Location | Impact |
|-------|----------|--------|
| `WindowToWallRatio` bypassed | `HouseParametersService.GenerateRoomLayout()` | Uses hardcoded `0.15` instead of style value |
| Material type mismatch | Frontend `GeometryRenderer.js` | Doesn't recognize all backend material types |

#### Medium Issues (Low Priority)

| Issue | Location | Impact |
|-------|----------|--------|
| GltfExporter ignores `Material.Color` | `GltfExporter.CreateMaterials()` | Uses hardcoded style switch, not parameter |
| IfcExporter wall props hardcoded | `IfcExporter.AddWallProperties()` | Always "Stucco" regardless of style |
| ObjExporter exports cube only | `ObjExporter.cs` | Legacy — ignores BuildingGeometry |
| `Design` stores keywords, not StyleTemplateId | `Design.cs` | Export re-parses; could theoretically change |
| TwoStoryLayoutStrategy redundant | `LayoutStrategies/` | Near-identical output to CubeLayoutStrategy |

#### Remaining Remediation Priority

1. Pass `WindowToWallRatio` to `GenerateRoomLayout()`
2. Normalize material types between backend/frontend

---

### Code Quality

**SQL Server connection validation** — Validate connection before use; fall back to SQLite if invalid. File: `Program.cs`.

---

## BIM Export Roadmap

### Current IFC Entity Status

| Entity | Status |
|---|---|
| IfcProject / IfcSite / IfcBuilding / IfcBuildingStorey | Done |
| IfcSpace (rooms) | Done |
| IfcWallStandardCase (extruded profile + Axis centreline) | Done |
| IfcWindow (with IfcOpeningElement + IfcRelVoidsElement + IfcRelFillsElement) | Done |
| IfcDoor (proximity-matched to host wall, 2ft tolerance) | Done |
| IfcSlab (floor + ceiling per storey) | Done |
| IfcRoof (with pitch/overhang) | Done |
| Body, Axis, Box representation sub-contexts | Done |
| IfcMaterialLayerSet | Not started |
| 2D plan representations | Not started |

### Phase 2: IfcOpenShell Post-Processing

Priority: Medium | Requires Python 3.x + pip

- IFC to glTF conversion via IfcConvert (alternative to SharpGLTF)
- IDS validation endpoint (`GET /api/designs/{id}/validate`)
- Clash detection endpoint (`GET /api/designs/{id}/clash`)

### Phase 4: Material and Classification

Priority: Low

- `IfcMaterialLayerSet` for walls (replacing single-string material)
- Uniclass / OmniClass classification codes on exported elements

### Phase 5: Frontend glTF Loading

Priority: Low

Load server-rendered GLB in Three.js viewer instead of generating geometry client-side.

---

## Validation Checklist (Run After Geometry Changes)

- [ ] `dotnet test` passes
- [ ] Generate a design with each style (Modern, Victorian, Brutalist)
- [ ] Export OBJ, IFC, GLB and verify they open in target software
- [ ] 3D viewer camera controls work
- [ ] No console errors in browser or backend logs
