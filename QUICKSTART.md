# Quick Start Guide

## Prerequisites
- .NET 8 SDK installed
- Node.js 18+ installed

## 3 Steps to Run

### 1. Start Backend
```bash
cd ArchitecturalDreamMachineBackend/ArchitecturalDreamMachineBackend
dotnet run
```
Wait for: `Now listening on: http://localhost:5095`

### 2. Start Frontend (New Terminal)
```bash
cd ArchitecturalDreamMachineFrontend
npm install   # first time only
npx expo start
```
Press **'w'** to open in browser.

### 3. Generate a Design
1. Lot size: `2500`
2. Style: `modern glass house`
3. Click **Generate Design**

## Troubleshooting

**Backend won't start?** — Verify .NET 8 SDK: `dotnet --version`. Check port 5095 not in use.

**Frontend won't load?** — Clear cache: `npx expo start --clear`. Try Chrome.

**Can't connect?** — Verify backend is running. Visit http://localhost:5095/swagger to test.

## What's Included

- 3 styles (Modern, Victorian, Brutalist)
- 5 building layouts (cube, L-shape, two-story, split-level, angled)
- 3D rotating model in browser
- 3 export formats: OBJ (mesh), IFC4 (BIM/Revit), GLB (web/Blender)
- API key authentication and rate limiting

**Next:** [USER_GUIDE.md](USER_GUIDE.md) for features, [HOUSE_LAYOUTS.md](HOUSE_LAYOUTS.md) for layouts, [DEVELOPER_SETUP.md](DEVELOPER_SETUP.md) for technical setup.
