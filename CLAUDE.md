# Claude Memory — Architectural Dream Machine

## Communication Style

Talk casually, no need to be stiff. Stay concise and precise while coding, but conversation can be relaxed.
Maybe even make a joke once in a while, feel free to adapt and develop a personality. 

---

## Project Overview

Turn text prompts into 3D architectural designs. User types a style/lot size, gets a rotating 3D model, can download for CAD or BIM software.

Current branch: `security/phase2-hardening`
Main branch: `master`

---

## Tech Stack

| Layer | Tech |
|---|---|
| Backend | C# / ASP.NET Core 8.0 / Entity Framework Core / SQLite |
| Frontend | React Native / Expo / Three.js 0.145.0 |
| BIM Export | xBIM Toolkit (IFC4), SharpGLTF Toolkit (GLB) |
| Auth | API key (`X-API-Key` header) — dev: auto-skipped if no key configured; set via user secrets or `API_KEY` env var |

**Backend port: 5095** (not 5162 — that was outdated)

---

## Project Structure

```
ArchitectCode/
├── ArchitecturalDreamMachineBackend/ArchitecturalDreamMachineBackend/   ← C# API
├── ArchitecturalDreamMachineFrontend/                                   ← React Native
├── scripts/ifc_to_gltf.py                                               ← Python utility
└── *.md                                                                  ← Docs (see below)
```

---

## Documentation Map

| File | Purpose |
|---|---|
| `README.md` | Project overview, quick start |
| `QUICKSTART.md` | 2-minute setup |
| `USER_GUIDE.md` | End-user walkthrough |
| `HOUSE_LAYOUTS.md` | 5 layout types explained |
| `3D_AND_EXPORT_GUIDE.md` | 3D viewer + OBJ/IFC/GLB exports |
| `DEVELOPER_SETUP.md` | Full technical setup, API reference, security |
| `ROADMAP.md` | Completed work + BIM/IFC enhancement plan |
| `TESTING_PLAN.md` | 15 manual test cases (layouts × styles) |
| `WINDOWS_DOORS_IMPLEMENTATION.md` | Implementation notes |

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
  └── IfcExporter          — IFC4 via xBIM, full BIM compliance (Phase 1 complete)
```

---

## IFC Export Status (Phase 1 Complete)

All Phase 1 BIM compliance items done in `Export/IfcExporter.cs`:
- ✅ `IfcWallStandardCase` with extruded profiles + Axis centrelines
- ✅ Windows: full Wall → `IfcRelVoidsElement` → Opening → `IfcRelFillsElement` → Window chain
- ✅ Doors: `IfcDoor` with proximity-matched host wall (2 ft tolerance)
- ✅ `IfcSlab` floor/ceiling per storey
- ✅ Body, Axis, Box `IfcGeometricRepresentationSubContext` registered

Remaining: Phase 2 (IfcOpenShell post-processing), Phase 3 (semantic BuildingModel), Phase 4 (material layers). All documented in `ROADMAP.md`.

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

All require `X-API-Key` header. Swagger: http://localhost:5095/swagger

---

## API Key Setup

**Development** — auth is auto-skipped when no key is configured, so you can hit the API freely. To test auth locally:
```bash
cd ArchitecturalDreamMachineBackend/ArchitecturalDreamMachineBackend
dotnet user-secrets set "ApiKey" "your-dev-key"
```
User secrets are stored in `~/.microsoft/usersecrets/` and are never committed to git.

**Production** — set the `API_KEY` environment variable in your hosting environment. Never put real keys in `appsettings.json` or any committed file.

---

## Tests

```bash
dotnet test
# Expected: 8 pass, 2 pre-existing failures (Assert.IsType BadRequest vs ObjectResult — unrelated to current work)
```

---

## Known Pre-existing Test Failures (not to fix unless asked)

- `Generate_InvalidLotSize_ReturnsBadRequest` — expects `BadRequestObjectResult`, gets `ObjectResult`
- `Generate_EmptyStylePrompt_ReturnsBadRequest` — same issue

---

## Architecture Decisions Made

- **Dual-output pattern**: Services produce both `GeometryData` (for Three.js rendering) AND typed elements (`WindowElement`, `DoorElement`) for BIM export — don't collapse these
- **Port 5095** is canonical — ignore any docs that say 5162
- **No Python dependency for exports** — GLB uses SharpGLTF (pure .NET), removed old Python reliance
- **CORS**: `AllowAnyOrigin` only in Development; production scoped to `appsettings.json → Cors:AllowedOrigins`
- **Rate limiting** on POST endpoints
