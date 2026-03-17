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

---

## Pending

### Style System Audit (2026-02-21)

Comprehensive review of style differentiation (Modern, Victorian, Brutalist).

**No breaking conflicts found** — styles don't clash during generation. However, several gaps cause styles to produce near-identical output despite different parameters.

#### Critical Issues

| Issue | Location | Impact |
|-------|----------|--------|
| `MaxWindowsPerRoom=5` clamps all styles | `ArchitecturalConstants.cs#L67` | Modern 30%, Victorian 20%, Brutalist 10% all produce 5 windows in typical rooms |
| `WindowToWallRatio` bypassed | `HouseParametersService.GenerateRoomLayout()` | Uses hardcoded `0.15` instead of style value |
| Parapets generated but discarded | `FlatRoofStrategy.CreateParapetWalls()` | `RoofGeometry` has no storage — Modern/Brutalist roofs identical |
| WindowStyle decorative only | `WindowService.cs` | `"small"/"ornate"/"large"` never affects geometry |
| Style matching duplicated 4× | `DesignsController.cs` lines 58, 160, 220, 280 | Must apply fixes in all locations |
| Material type mismatch | Frontend `GeometryRenderer.js` | Doesn't recognize `"stucco"`, `"wood siding"` |

#### Medium Issues

| Issue | Location | Impact |
|-------|----------|--------|
| GltfExporter ignores `Material.Color` | `GltfExporter.CreateMaterials()` | Uses hardcoded style switch, not parameter |
| IfcExporter wall props hardcoded | `IfcExporter.AddWallProperties()` | Always "Stucco" regardless of style |
| ObjExporter exports cube only | `ObjExporter.cs` | Legacy — ignores BuildingGeometry |
| `Design` stores keywords, not StyleTemplateId | `Design.cs` | Export re-parses; could theoretically change |
| TwoStoryLayoutStrategy redundant | `LayoutStrategies/` | Identical output to CubeLayoutStrategy |

#### Remediation Priority

1. Raise or remove `MaxWindowsPerRoom` constant
2. Pass `WindowToWallRatio` to `GenerateRoomLayout()`
3. Add `Parapets` collection to `RoofGeometry`, return from strategy
4. Extract style resolution to shared service (eliminate 4× duplication)
5. Implement `WindowStyle` → dimensions mapping
6. Normalize material types between backend/frontend

#### Documentation Inconsistencies Found

- WALL_FACE_FIX_PLAN.md claimed SplitLevelLayoutStrategy ignores stories > 2 — code correctly loops all floors
- TESTING_PLAN.md dated Nov 2025 shows "0 / 15" tests completed — appears stale

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
