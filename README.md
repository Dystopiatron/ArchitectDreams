# Architectural Dream Machine

> THIS IS A WIP/NOT FINALIZED!!

Turn your ideas into 3D house designs instantly. Type a style, see a rotating 3D model, download for professional CAD or BIM software.

---

## Quick Start

**Terminal 1 — Start Backend:**
```bash
cd ArchitecturalDreamMachineBackend/ArchitecturalDreamMachineBackend
dotnet run
```

**Terminal 2 — Start Frontend:**
```bash
cd ArchitecturalDreamMachineFrontend
npx expo start
```

Press **`w`** to open in your web browser → http://localhost:8081

---

## Documentation

**New Users:**
- **[QUICKSTART.md](QUICKSTART.md)** — Get running in 2 minutes
- **[USER_GUIDE.md](USER_GUIDE.md)** — Complete walkthrough with examples
- **[HOUSE_LAYOUTS.md](HOUSE_LAYOUTS.md)** — Understanding the 5 layout types
- **[3D_AND_EXPORT_GUIDE.md](3D_AND_EXPORT_GUIDE.md)** — 3D viewer, camera controls, OBJ/IFC/GLB export and import

**Developers:**
- **[DEVELOPER_SETUP.md](DEVELOPER_SETUP.md)** — Technical setup, architecture, API reference, security
- **[ROADMAP.md](ROADMAP.md)** — Completed work and planned enhancements (BIM, IFC, semantic model)

---

## Features

- **Natural Language Input** — "Modern glass house" → instant 3D model
- **3D Visualization** — Rotating house with roof, windows, doors, interior walls
- **5 Layout Types** — Cube, L-shape, two-story, split-level, angled
- **3 Export Formats** — OBJ (mesh), IFC4 (BIM/Revit), GLB (web/Blender)
- **Design History** — All designs saved to database
- **3 Built-in Styles** — Modern, Victorian, Brutalist
- **Security** — API key auth, rate limiting, HSTS, input validation

---

## Architecture

```
React Native / Expo (Frontend)
  Three.js — interactive 3D rendering
         ↓ HTTP (port 5095)
ASP.NET Core 8.0 (Backend)
  PromptParser → HouseParametersService → DesignOrchestrationService
         ↓ Entity Framework Core
SQLite (default) / SQL Server (optional)
```

**Tech Stack:**
- Backend: C# / ASP.NET Core 8.0 / Entity Framework / SQLite
- Frontend: React Native / Three.js 0.145.0 / Expo
- Export: xBIM Toolkit (IFC4), SharpGLTF Toolkit (GLB)

---

## Example Usage

1. Lot Size: `2500` sq ft
2. Style: `modern minimalist with large windows`
3. Click **Generate Design** → see 3D model
4. Download OBJ, IFC, or GLB

**Style Keywords:**
- Modern: `modern`, `minimalist`, `glass`, `contemporary`
- Victorian: `victorian`, `ornate`, `classic`, `traditional`
- Brutalist: `brutalist`, `concrete`, `industrial`, `raw`

---

## Requirements

- .NET 8 SDK
- Node.js 18+
- Web browser (Chrome/Firefox recommended)
- macOS, Windows, or Linux

---

## Testing

```bash
cd ArchitecturalDreamMachineBackend/ArchitecturalDreamMachineBackend
dotnet test
```

Tests cover: API validation, prompt parsing, geometry generation, controller behavior.

---

## API Endpoints

```http
POST /api/designs/generate          # Generate design from prompt
GET  /api/designs                   # List all designs
GET  /api/designs/{id}              # Get design by ID
GET  /api/designs/{id}/export       # Download OBJ file
GET  /api/designs/{id}/export/ifc   # Download IFC4 file (BIM)
GET  /api/designs/{id}/export/gltf  # Download GLB file (web/3D)
```

All endpoints require `X-API-Key` header.
API docs: http://localhost:5095/swagger (when backend running)

---

## License

Educational / personal use

---

## Credits

Built with ASP.NET Core, React Native, Expo, Three.js, Entity Framework Core, xBIM Toolkit, SharpGLTF Toolkit.
