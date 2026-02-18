# Architectural Dream Machine — Roadmap

This document tracks completed work and planned enhancements. Items are organized by status and priority.

---

## Completed Work

### Code Quality & Architecture

| Item | Description |
|---|---|
| ✅ IDesignOrchestrationService | Interface added; DesignOrchestrationService updated; controller and tests use interface |
| ✅ IGeometryService / ILayoutService / IRoofService | All core services have interfaces for testability |
| ✅ Extract Magic Numbers | `Constants/ArchitecturalConstants.cs` with AspectRatio, PitchDivisor, WindowRatios, etc. |
| ✅ Fix Null Handling | HouseParameters now has consistent non-empty defaults; redundant null coalescing removed |
| ✅ Extract HouseParametersService | Parameter calculation extracted from DesignsController to `HouseParametersService` |
| ✅ Windows & Interior Walls | `WindowService` and `InteriorWallService` replace TODO placeholders |

### API & Validation

| Item | Description |
|---|---|
| ✅ DTOs for API Responses | `Models/ApiResponses.cs`: `GenerateResponse`, `ErrorResponse`, `DesignSummary` |
| ✅ FluentValidation | `Validators/GenerateRequestValidator.cs`; removed manual if-chain from controller |
| ✅ Input Sanitization | `PromptParser.Sanitize()` strips dangerous chars; `[MaxLength]` on request DTOs |
| ✅ Environment Config for API URL | `config/api.js` centralises backend URL; `.env.example` documents `REACT_NATIVE_API_URL` |
| ✅ Cross-Platform Picker | `CrossPlatformPicker.js` replaces native `<select>` for React Native compatibility |

### Security Hardening (Phase 1 & 2)

| Item | Description |
|---|---|
| ✅ XSS/CORS | CORS scoped to configured origins; `AllowAnyOrigin` only in `Development` |
| ✅ HSTS | HTTP Strict Transport Security enabled in non-development environments |
| ✅ Rate Limiting | Applied to POST endpoints via ASP.NET Core middleware |
| ✅ API Key Authentication | `X-API-Key` header required on all endpoints; keys in `appsettings.json` |
| ✅ Pinned Dependencies | NuGet + npm packages pinned to verified versions |
| ✅ Geometry Validation | Input bounds validated before geometry generation |

### Export Features

| Item | Description |
|---|---|
| ✅ OBJ Export | `GET /api/designs/{id}/export` — Wavefront OBJ for AutoCAD, Blender, SketchUp |
| ✅ IFC Export | `GET /api/designs/{id}/export/ifc` — IFC4 via xBIM Toolkit; full IfcProject hierarchy |
| ✅ GLB/glTF Export | `GET /api/designs/{id}/export/gltf` — Binary GLB via SharpGLTF Toolkit; PBR materials, transparent glass, style-specific colors |

### 3D Viewer (Frontend)

| Item | Description |
|---|---|
| ✅ Interactive Camera | Zoom, rotate, preset views (Top, Front, Side, Perspective) |
| ✅ Dark Theme | Reduced eye strain UI |
| ✅ Auto-Rotate Fix | `autoRotateRef` resolves stale closure in Three.js animation loop |

---

## Pending — Code Quality

### 🔴 Fix Stale Closure Bug in HouseViewer3D

Already noted as fixed above (`autoRotateRef`), but verify the animated loop uses the ref consistently.

**Files:** `ArchitecturalDreamMachineFrontend/components/HouseViewer3D.js`

### 🟠 Add Error Handling for SQL Server Connection String

Validate SQL Server connection before use; fall back to SQLite if invalid.

**Files:** `ArchitecturalDreamMachineBackend/Program.cs`

```csharp
var sqlServerConnection = Environment.GetEnvironmentVariable("SQL_SERVER_CONNECTION_STRING");
if (!string.IsNullOrEmpty(sqlServerConnection))
{
    try { options.UseSqlServer(sqlServerConnection); }
    catch { /* log, fall back to SQLite */ }
}
```

---

## BIM Export Roadmap

Current IFC exports produce valid IFC4 structure but have gaps that prevent proper import in professional BIM tools (Revit, ArchiCAD). The following phases address these gaps progressively.

### Current IFC Status

| Entity | Status |
|---|---|
| IfcProject / IfcSite / IfcBuilding | ✅ Complete |
| IfcBuildingStorey | ✅ Complete |
| IfcSpace (rooms) | ✅ Complete |
| IfcRoof (with pitch/overhang) | ✅ Complete |
| IfcWall | ✅ `IfcWallStandardCase` with extruded profile and Axis centreline |
| IfcWindow | ✅ `IfcOpeningElement` → `IfcRelVoidsElement` (voids wall) → `IfcRelFillsElement` (fills opening) |
| IfcDoor | ✅ `IfcDoor` with `IfcRelVoidsElement` to host interior wall; position-matched |
| IfcSlab (floors/ceilings) | ✅ Per-storey floor slab + inter-storey ceiling slabs |
| IfcOpeningElement | ✅ Full Wall-Opening-Filling chain for both windows and doors |
| Representation sub-contexts | ✅ Body, Axis, Box sub-contexts registered under Model parent |
| Material layers | ❌ Single string, no IfcMaterialLayerSet |
| 2D Plan representations | ❌ Only 3D Body context |

---

### Phase 1: Enhanced xBIM IFC Compliance ✅ COMPLETE

All Phase 1 items are implemented in `Export/IfcExporter.cs`.

#### ✅ 1.1 IfcOpeningElement for Windows

Full Wall-Opening-Filling chain: `IfcRelVoidsElement` links each opening to its host exterior wall (looked up by `WallDirection`); `IfcRelFillsElement` links the window to the opening.

#### ✅ 1.2 IfcDoor Entities

`DoorElement.cs` carries explicit (X, Z) position. `CreateDoorsWithOpenings` uses `FindHostWallForDoor` with proximity matching (Manhattan distance, 2 ft tolerance) to link each door to its host interior wall via `IfcRelVoidsElement` + `IfcRelFillsElement`.

#### ✅ 1.3 IfcSlab for Floors and Ceilings

`CreateFloorSlabs` generates per-storey floor slabs (type `FLOOR`) and inter-storey ceiling slabs with extruded `IfcRectangleProfileDef` geometry.

#### ✅ 1.4 Proper Wall Geometry with Extrusion

`CreateProperWall` creates `IfcWallStandardCase` with line-based placement, extruded `IfcRectangleProfileDef` body representation, and `IfcPolyline` axis representation. Wall thickness: 0.5 ft (exterior).

#### ✅ 1.5 Multiple Representation Contexts

`Body`, `Axis`, and `Box` `IfcGeometricRepresentationSubContext` objects registered under the root Model context in `CreateProject`.


### Phase 2: IfcOpenShell Post-Processing Pipeline

**Priority: MEDIUM | Requires Python 3.x + pip**

Adds optional Python-based enhancement and format conversion.

#### 2.1 Install IfcOpenShell

```bash
python3 -m venv venv
source venv/bin/activate
pip install ifcopenshell ifcpatch ifcclash ifctester
# ifcconvert binary: download from blenderbim.org/docs-python/ifcconvert
```

#### 2.2 IFC → glTF via IfcConvert (alternative to SharpGLTF)

Produce glTF from IFC for guaranteed geometry fidelity:
```csharp
// Export/IfcConverter.cs
public interface IIfcConverter
{
    Task<byte[]> ConvertToGltfAsync(byte[] ifcData);
}
```

#### 2.3 IDS Validation Endpoint

`GET /api/designs/{id}/validate` — Validate export against an IDS specification.

IDS file: `Validation/adm_requirements.ids`

#### 2.4 Clash Detection Endpoint

`GET /api/designs/{id}/clash` — Detect geometry overlaps between rooms and walls.

**Estimated effort:** 8–12 hours

---

### ✅ Phase 3: Enhanced Semantic Model — COMPLETE

**Priority: MEDIUM | No external dependencies**

Enriches the data model for full BIM semantics before export.

#### 3.1 WallSegment Entity

```csharp
// Geometry/WallSegment.cs
public class WallSegment
{
    public double StartX, StartZ, EndX, EndZ, Height, Thickness;
    public WallType Type;        // Exterior, Interior, Partition
    public bool IsLoadBearing;
    public List<Opening> Openings;  // Windows and doors with positions
}
```

#### 3.2 BuildingModel Aggregate

Replaces loose `BuildingGeometry` with a full semantic aggregate:

```csharp
// Models/BuildingModel.cs
public class BuildingModel
{
    public List<Floor> Floors;
    public List<WallSegment> Walls;
    public List<DoorElement> Doors;
    public List<WindowElement> Windows;
    public List<Slab> Slabs;
    public RoofAssembly Roof;
    public double GrossFloorArea;
}
```

#### 3.3 Update Services

| Service | Enhancement |
|---|---|
| `LayoutService` | Produce `WallSegment` entities with start/end coordinates |
| `WindowService` | Link `WindowElement` to parent wall |
| `InteriorWallService` | Track `DoorElement` positions explicitly |
| `DesignOrchestrationService` | Build complete `BuildingModel` aggregate |

**Estimated effort:** 8–12 hours

---

### Phase 4: Material and Classification Support

**Priority: LOW**

Adds professional material layer definitions and building classification codes.

#### 4.1 Material Layer Sets

`IfcMaterialLayerSet` for walls (gypsum + stud + gypsum, exterior sheathing, etc.) replacing the current single-string material field.

**New file:** `Geometry/MaterialLayer.cs`

#### 4.2 Uniclass / OmniClass Classification

Attach standard classification codes to exported IFC elements.

**New file:** `Classification/BuildingClassification.cs`

**Estimated effort:** 4–8 hours

---

### Phase 5: Frontend glTF Loading (Optional)

**Priority: LOW**

Allow the Three.js viewer to load server-rendered GLB instead of generating geometry client-side.

```javascript
// HouseViewer3D.js
if (houseParams.gltfUrl) {
  await loadGltfFromUrl(houseGroup, houseParams.gltfUrl);
} else {
  GeometryRenderer.renderBuilding(houseGroup, houseParams.geometry);
}
```

**Estimated effort:** 4–8 hours

---

## IFC Validation Checklist (Phase 1 Success Criteria)

- [ ] Windows appear as proper openings in walls when imported to Revit
- [ ] Doors are `IfcDoor` entities, not just wall gaps
- [ ] Floors are `IfcSlab` entities
- [ ] Walls have proper thickness and line-based extrusion geometry
- [ ] IFC passes basic IDS validation (`ifctester`)
- [ ] All existing unit tests continue to pass after changes

---

## Revit Interoperability Note

IFC is the bridge to Revit — `.rvt` format is proprietary but Revit imports IFC natively (File → Open → IFC). Use IFC4 for best Revit support. Phases 1–3 above are sufficient for a Revit-compatible export.

---

## Testing Checklist (Run After Any Geometry Change)

- [ ] All unit tests pass: `dotnet test`
- [ ] Generate a design with each style (Modern, Victorian, Brutalist)
- [ ] Export OBJ → verify opens in Blender
- [ ] Export IFC → verify opens in BIM Vision / xBIM Xplorer
- [ ] Export GLB → verify opens in https://gltf-viewer.donmccurdy.com/
- [ ] 3D viewer camera controls work (rotate, zoom, presets)
- [ ] No console errors in browser DevTools
- [ ] No errors in backend logs

---

*Last updated: February 2026*
