# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Communication Style

Talk casually, no need to be stiff. Stay concise and precise while coding, but conversation can be relaxed.
Maybe even make a joke once in a while, feel free to adapt and develop a personality.

---

## Project Overview

Turn text prompts into 3D architectural designs. User types a style/lot size, gets a rotating 3D model, can download for CAD or BIM software.

Main branch: `master`

---

## Tech Stack

| Layer | Tech |
|---|---|
| Backend | C# / ASP.NET Core 8.0 / Entity Framework Core / SQLite |
| Frontend | React Native / Expo / Three.js 0.145.0 |
| BIM Export | xBIM Toolkit (IFC4), SharpGLTF Toolkit (GLB) |
| Auth | API key (`X-API-Key` header) — dev: auto-skipped if no key configured |

**Backend port: 5095** (not 5162 — that was outdated)

---

## Build & Run Commands

### Backend

```bash
# All from ArchitecturalDreamMachineBackend/ArchitecturalDreamMachineBackend/
dotnet build
dotnet run              # Starts on http://localhost:5095
dotnet test             # 10 tests expected to pass
dotnet test --filter "FullyQualifiedName~PromptParserTests"  # Run a single test class
```

Swagger UI: http://localhost:5095/swagger

### Frontend

```bash
# All from ArchitecturalDreamMachineFrontend/
npm install
npx expo start --web    # 3D viewer only works on web (localhost:8081)
npx jest                # Roof geometry tests
```

The 3D viewer checks `Platform.OS === 'web'` — iOS/Android show a text parameter summary instead.

---

## Project Structure

```
ArchitectCode/
├── ArchitecturalDreamMachineBackend/ArchitecturalDreamMachineBackend/   ← C# API
├── ArchitecturalDreamMachineFrontend/                                   ← React Native
├── scripts/ifc_to_gltf.py                                               ← Python utility
└── *.md                                                                  ← Docs
```

---

## Key Backend Services

```
PromptParser → HouseParametersService → DesignOrchestrationService
  ├── LayoutService        — building sections & exterior walls
  ├── GeometryService      — 3D mesh primitives
  ├── RoofService          — flat & gabled strategies
  ├── WindowService        — windows on all exterior walls (dual output: GeometryData + WindowElement)
  └── InteriorWallService  — partition walls with door openings (dual output: GeometryData + DoorElement)
Export/
  ├── GltfExporter         — GLB via SharpGLTF, PBR materials, transparent glass
  └── IfcExporter          — IFC4 via xBIM (see ROADMAP.md for phase status)
```

All services registered as Scoped DI with `I<Name>Service` / `<Name>Service` interface+impl pairs. Layout strategies (`ILayoutStrategy`) and roof strategies (`IRoofStrategy`) use the strategy pattern.

---

## Coordinate System

All measurements are in **feet**. The 3D space is **Y-up**:

- **Y** = up (height, 0 = ground)
- **X** = left/right (width)
- **Z** = front/back (depth)

Section positioning uses **center point** (X, Y, Z where Y = half-height above ground).

Wall face orientations via `rotationY`:
| Face | Direction | rotationY |
|------|-----------|-----------|
| Front | +Z | `0` |
| Back | −Z | `π` |
| Right | +X | `−π/2` |
| Left | −X | `π/2` |

All default constants live in `Constants/ArchitecturalConstants.cs` — always add new magic numbers there.

---

## Rendering Architecture (Frontend)

The 3D viewer uses a layered approach — this is non-obvious:

1. **Building sections** render with `THREE.BackSide` so you see the inner surface through window/door holes
2. **WallFaces** are `THREE.ShapeGeometry` panels with holes punched for each opening — these are the visible exterior walls
3. **Windows** use `MeshPhysicalMaterial` with `transmission: 0.9, opacity: 0.3` for glass effect

Component hierarchy: `HouseViewer3D` → `SceneManager` (scene/camera/lights) → `GeometryRenderer` (backend DTOs → Three.js meshes). Camera uses `CameraController` with spherical orbit coordinates.

---

## API Endpoints

```
POST /api/designs/generate
GET  /api/designs
GET  /api/designs/{id}
GET  /api/designs/{id}/export       → OBJ
GET  /api/designs/{id}/export/ifc   → IFC4
GET  /api/designs/{id}/export/gltf  → GLB
```

All require `X-API-Key` header in production. Swagger: http://localhost:5095/swagger

---

## API Key Setup

**Development** — auth is auto-skipped when no key is configured, so you can hit the API freely. To test auth locally:
```bash
cd ArchitecturalDreamMachineBackend/ArchitecturalDreamMachineBackend
dotnet user-secrets set "ApiKey" "your-dev-key"
```

**Production** — set the `API_KEY` environment variable. Never put real keys in `appsettings.json`.

---

## Testing

**Backend** (xUnit + Moq): Tests are co-located in `Tests/` inside the main project (not a separate test project). 3 test classes: `DesignsControllerTests` (4), `GeometryTests` (2 active + 4 skipped), `PromptParserTests` (4).

```bash
dotnet test                                           # All tests
dotnet test --filter "FullyQualifiedName~GeometryTests"  # Single class
```

**Frontend** (Jest): `tests/roofGeometry.test.js` — 5 roof pitch math tests. Run with `npx jest`.

**CI**: GitHub Actions (`.github/workflows/ci.yml`) runs on push/PR to `master` — builds both backend and frontend, runs `dotnet test`.

---

## Architecture Decisions

- **Dual-output pattern**: Services produce both `GeometryData` (flat vertex/index arrays for Three.js) AND typed elements (`WindowElement`, `DoorElement`, `WallSegment`) for BIM export — don't collapse these into one form
- **Port 5095** is canonical — ignore any docs that say 5162
- **No Python dependency for exports** — GLB uses SharpGLTF (pure .NET)
- **CORS**: `AllowAnyOrigin` only in Development; production scoped to `appsettings.json → Cors:AllowedOrigins`
- **Rate limiting** on POST endpoints (30/min fixed window)
- **4KB max request body** enforced at Kestrel level
