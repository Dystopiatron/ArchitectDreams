# Developer Setup Guide

Complete technical reference for setting up, running, and extending the Architectural Dream Machine.

---

## Prerequisites

- **macOS** (Ventura or later recommended; Windows/Linux supported with .NET 8)
- **.NET 8 SDK** — [dotnet.microsoft.com](https://dotnet.microsoft.com/download)
- **Node.js 18+** — [nodejs.org](https://nodejs.org/)
- **Visual Studio Code** (optional) — [code.visualstudio.com](https://code.visualstudio.com/)

Verify:
```bash
dotnet --version  # Should show 8.0.x or later
node --version    # Should show v18.x or higher
```

---

## Backend Setup

```bash
cd ArchitecturalDreamMachineBackend/ArchitecturalDreamMachineBackend

dotnet restore    # Restore NuGet packages
dotnet build      # Verify build (0 errors expected)
dotnet test       # Run tests (8 pass; 2 pre-existing failures unrelated to current work)
dotnet run        # Start the API server
```

Expected output:
```
Now listening on: http://localhost:5095
```

Keep this terminal open. The backend must be running for the frontend to work.

### Environment Variables

| Variable | Default | Description |
|---|---|---|
| `SQL_SERVER_CONNECTION_STRING` | *(unset)* | Use SQL Server instead of SQLite |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Set to `Development` to relax CORS |

Example (SQL Server):
```bash
export SQL_SERVER_CONNECTION_STRING="Server=localhost;Database=ArchitecturalDreamMachine;..."
dotnet run
```

### Database

Default: SQLite (`architecturaldreammachine.db`) — auto-created on first run with seeded style templates.

Reset:
```bash
rm architecturaldreammachine.db
dotnet run
```

### API Key Authentication

All API calls require the header:
```
X-API-Key: your-api-key-here
```

See `appsettings.json` → `ApiKeys` for allowed keys. Swagger UI includes an "Authorize" button to set the key for interactive testing.

---

## Frontend Setup

**New terminal:**
```bash
cd ArchitecturalDreamMachineFrontend

npm install        # First time only
npx expo start
```

Then press:
- **`w`** — Open in web browser at http://localhost:8081 (recommended for macOS)
- **`i`** — Open in iOS Simulator (requires Xcode)
- **`a`** — Open in Android Emulator (requires Android Studio)

### API URL Configuration

The frontend reads the backend URL from `config/api.js`. To override:

```bash
# .env (copy from .env.example)
REACT_NATIVE_API_URL=http://localhost:5095
```

For a **physical iOS/Android device**, use your Mac's LAN IP:
```bash
ifconfig | grep "inet " | grep -v 127.0.0.1
# Example: inet 192.168.1.100
REACT_NATIVE_API_URL=http://192.168.1.100:5095
```

---

## Architecture

```
React Native / Expo (Frontend)
  Three.js 0.145.0 — 3D rendering
  Axios — HTTP client
         ↓ HTTP/JSON (port 5095)
ASP.NET Core 8.0 API (Backend)
  PromptParser → HouseParametersService → DesignOrchestrationService
    ├── LayoutService       — building sections & wall segments
    ├── GeometryService     — 3D mesh primitives
    ├── RoofService         — flat & gabled roof strategies
    ├── WindowService       — window placement on exterior walls
    └── InteriorWallService — partition walls with door openings
  Export
    ├── IfcExporter         — IFC4 via xBIM Toolkit
    └── GltfExporter        — GLB via SharpGLTF Toolkit
         ↓ Entity Framework Core
SQLite (default) / SQL Server (optional)
```

### Backend Project Structure

```
ArchitecturalDreamMachineBackend/
├── Constants/
│   └── ArchitecturalConstants.cs      # Shared numeric constants
├── Controllers/
│   └── DesignsController.cs           # REST endpoints
├── Data/
│   ├── AppDbContext.cs                # EF Core context + seeding
│   ├── Design.cs                      # Design entity
│   └── StyleTemplate.cs               # Style template entity
├── Export/
│   ├── IGltfExporter.cs / GltfExporter.cs   # GLB export (SharpGLTF)
│   └── IIfcExporter.cs  / IfcExporter.cs    # IFC4 export (xBIM)
├── Geometry/
│   ├── DoorElement.cs / WindowElement.cs    # Opening elements
│   └── HouseParameters.cs                   # Core parameter model
├── Models/
│   ├── ApiResponses.cs                # Typed DTO responses
│   └── GeometryData.cs                # 3D geometry data
├── RoofStrategies/
│   ├── FlatRoofStrategy.cs
│   └── GabledRoofStrategy.cs
├── Services/
│   ├── DesignOrchestrationService.cs  # Orchestrates geometry pipeline
│   ├── GeometryService.cs             # Mesh primitives
│   ├── HouseParametersService.cs      # Parameter extraction
│   ├── InteriorWallService.cs         # Interior wall generation
│   ├── LayoutService.cs               # Building section layout
│   ├── RoofService.cs                 # Roof geometry
│   └── WindowService.cs               # Window geometry
├── Tests/
│   └── DesignsControllerTests.cs
├── Validators/
│   └── GenerateRequestValidator.cs    # FluentValidation rules
├── PromptParser.cs                    # Keyword → style mapping
└── Program.cs                         # DI registration, middleware
```

### Frontend Project Structure

```
ArchitecturalDreamMachineFrontend/
├── components/
│   ├── CrossPlatformPicker.js   # Web + native picker component
│   └── HouseViewer3D.js         # Three.js 3D viewer
├── config/
│   └── api.js                   # Centralized API URL config
├── screens/
│   └── MainScreen.js            # Main UI
├── .env.example                 # Environment variable template
├── App.js                       # Navigation root
└── package.json
```

---

## API Reference

Base URL: `http://localhost:5095`

All endpoints require: `X-API-Key: <key>`

Interactive docs: http://localhost:5095/swagger

### POST /api/designs/generate

Generate a new architectural design from a text prompt.

**Request:**
```json
{
  "lotSize": 2500,
  "stylePrompt": "modern minimalist with large windows",
  "buildingShapeOverride": "l-shape",
  "storiesOverride": 2
}
```

| Field | Type | Constraints |
|---|---|---|
| `lotSize` | number | > 0 |
| `stylePrompt` | string | Required, max 500 chars |
| `buildingShapeOverride` | string | Optional: `rectangular`, `l-shape`, `split-level`, `angled` |
| `storiesOverride` | integer | Optional: 1–10 |

**Response `200`:**
```json
{
  "houseParameters": {
    "lotSize": 2500,
    "roofType": "flat",
    "windowStyle": "large",
    "roomCount": 5,
    "stories": 2
  },
  "geometry": { ... },
  "designId": 42,
  "styleName": "Modern"
}
```

### GET /api/designs

List all saved designs (paginated).

```
GET /api/designs?page=1&pageSize=20
```

### GET /api/designs/{id}

Get a specific design by ID.

### GET /api/designs/{id}/export

Download the design as a Wavefront OBJ file.

```
GET /api/designs/42/export
→ Content-Type: model/obj
→ design_42.obj
```

### GET /api/designs/{id}/export/ifc

Download the design as an IFC4 file (for Revit, ArchiCAD, BIM workflows).

```
GET /api/designs/42/export/ifc
→ Content-Type: application/x-step
→ design_42.ifc
```

### GET /api/designs/{id}/export/gltf

Download the design as a GLB binary (for Three.js, Blender, web viewers).

```
GET /api/designs/42/export/gltf
→ Content-Type: model/gltf-binary
→ design_42.glb
```

**Features:** PBR materials, transparent glass for large-window styles, style-specific color mapping.

---

## Running Tests

**Backend (xUnit):**
```bash
cd ArchitecturalDreamMachineBackend/ArchitecturalDreamMachineBackend
dotnet test --verbosity normal
```

Tests cover: controller validation, prompt parsing, geometry generation.

**Frontend (Jest):**
```bash
cd ArchitecturalDreamMachineFrontend
npm test
```

---

## Security Configuration

Security hardening is applied in `Program.cs`. Key settings:

| Feature | Configuration |
|---|---|
| CORS | `appsettings.json` → `Cors:AllowedOrigins`; all origins allowed in `Development` |
| API Key Auth | `appsettings.json` → `ApiKeys` |
| HSTS | Enabled in non-development environments |
| Rate Limiting | Applied to POST endpoints via middleware |
| Input Validation | FluentValidation on `GenerateRequest`; sanitization in `PromptParser` |
| Swagger | Available in all environments; requires API key |

To add an API key for local testing:
```json
// appsettings.Development.json
{
  "ApiKeys": ["dev-key-1234"]
}
```

---

## Adding a New Architectural Style

1. Open `Data/AppDbContext.cs`
2. Add a new entry in the `OnModelCreating` seed data:
```csharp
new StyleTemplate
{
    Name = "Scandinavian",
    Keywords = "scandinavian,nordic,minimal,birch",
    RoofType = "flat",
    WindowStyle = "large",
    PrimaryColor = "#F5F0EB",
    // ...
}
```
3. Delete `architecturaldreammachine.db` and restart the backend.

---

## Troubleshooting

### Backend won't start — port already in use

```bash
# Free port 5095
lsof -ti:5095 | xargs kill -9
dotnet run
```

### Database errors

```bash
rm architecturaldreammachine.db
dotnet run
```

### Frontend can't connect to backend

1. Verify backend shows `Now listening on: http://localhost:5095`
2. Check http://localhost:5095/swagger is responsive
3. Confirm `REACT_NATIVE_API_URL` is set correctly in `.env`
4. For physical devices, use your Mac's LAN IP (not `localhost`)

### Expo cache issues

```bash
npx expo start --clear
# or
rm -rf node_modules && npm install && npx expo start --clear
```

### React Native package warnings

```bash
npm install react-native-screens@~4.16.0
```

---

## Development Tips

- Use Swagger UI (`/swagger`) to test API endpoints without needing the frontend running
- Enable Hot Reload in Expo for faster frontend iteration
- Backend logs include detailed geometry pipeline output (section count, window count, etc.)
- Use browser DevTools console to inspect geometry data passed to Three.js
