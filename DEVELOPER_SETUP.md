# Developer Setup Guide

Technical reference for setting up and extending the Architectural Dream Machine.

---

## Prerequisites

- **.NET 8 SDK** — [dotnet.microsoft.com](https://dotnet.microsoft.com/download)
- **Node.js 18+** — [nodejs.org](https://nodejs.org/)

```bash
dotnet --version  # 8.0.x or later
node --version    # v18.x or higher
```

---

## Backend Setup

```bash
cd ArchitecturalDreamMachineBackend/ArchitecturalDreamMachineBackend
dotnet restore
dotnet build
dotnet test       # 10 pass, 4 skipped (stretch goals)
dotnet run        # Starts on http://localhost:5095
```

Swagger UI: http://localhost:5095/swagger

### Database

SQLite by default (`architecturaldreammachine.db`), auto-created on first run with seeded style templates. To reset: delete the .db file and restart.

For SQL Server, set `SQL_SERVER_CONNECTION_STRING` env var.

### API Key Authentication

In development, auth is auto-skipped when no key is configured. To test auth locally:

```bash
dotnet user-secrets set "ApiKey" "your-dev-key"
```

For production, set the `API_KEY` environment variable.

---

## Frontend Setup

```bash
cd ArchitecturalDreamMachineFrontend
npm install
npx expo start
```

Press **`w`** to open in browser (http://localhost:8081). The 3D viewer only works on web — iOS/Android show a text parameter summary.

### API URL

The frontend reads from `config/api.js`, defaulting to `http://localhost:5095`. Override via:

```bash
# .env
REACT_NATIVE_API_URL=http://localhost:5095
```

For physical devices, use your Mac's LAN IP instead of localhost.

---

## Architecture

```
React Native / Expo (Frontend)
  Three.js 0.145.0 — 3D rendering
         | HTTP/JSON (port 5095)
ASP.NET Core 8.0 API (Backend)
  PromptParser -> HouseParametersService -> DesignOrchestrationService
    |- LayoutService       — building sections & wall segments
    |- GeometryService     — 3D mesh primitives
    |- RoofService         — flat & gabled roof strategies
    |- WindowService       — window placement on exterior walls
    |- InteriorWallService — partition walls with door openings
    '- WallFaceService     — perforated wall panels with cutouts
  Export
    |- IfcExporter         — IFC4 via xBIM Toolkit
    '- GltfExporter        — GLB via SharpGLTF Toolkit
         | Entity Framework Core
SQLite (default) / SQL Server (optional)
```

---

## API Reference

Base URL: `http://localhost:5095` | Interactive docs: http://localhost:5095/swagger

### POST /api/designs/generate

```json
{
  "lotSize": 2500,
  "stylePrompt": "modern minimalist with large windows",
  "buildingShapeOverride": "l-shape",
  "storiesOverride": 2
}
```

| Field | Type | Notes |
|---|---|---|
| `lotSize` | number | Required, > 0 |
| `stylePrompt` | string | Required, max 500 chars |
| `buildingShapeOverride` | string | Optional: `rectangular`, `l-shape`, `split-level`, `angled` |
| `storiesOverride` | integer | Optional: 1-10 |

### GET /api/designs

List saved designs. Supports `?page=1&pageSize=20`.

### GET /api/designs/{id}/export

OBJ mesh export.

### GET /api/designs/{id}/export/ifc

IFC4 BIM export.

### GET /api/designs/{id}/export/gltf

GLB binary with PBR materials and transparent glass.

---

## Tests

**Backend** (xUnit + Moq): Tests live in `Tests/` inside the main project (not a separate test project).

```bash
dotnet test                                              # All tests
dotnet test --filter "FullyQualifiedName~PromptParserTests"  # Single class
```

**Frontend** (Jest):

```bash
cd ArchitecturalDreamMachineFrontend
npx jest
```

**CI**: GitHub Actions (`.github/workflows/ci.yml`) runs on push/PR to `master`.

---

## Security

| Feature | Details |
|---|---|
| CORS | Scoped origins in production (`appsettings.json`), all origins in Development |
| API Key | `X-API-Key` header on all endpoints |
| HSTS | Enabled in non-development environments |
| Rate Limiting | 30/min fixed window on POST endpoints |
| Input Validation | FluentValidation on requests, sanitization in PromptParser |
| Request Size | 4KB max body at Kestrel level |

---

## Adding a New Style

1. Add a `StyleTemplate` entry in `Data/AppDbContext.cs` seed data
2. Delete `architecturaldreammachine.db` and restart

---

## Troubleshooting

**Port in use:** `lsof -ti:5095 | xargs kill -9`

**Database errors:** Delete `architecturaldreammachine.db` and restart.

**Frontend can't connect:** Verify backend shows `Now listening on: http://localhost:5095`. Check http://localhost:5095/swagger.

**Expo cache:** `npx expo start --clear`
