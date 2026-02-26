# Windows and Doors Implementation

## Status

Windows and doors are **implemented** in the current codebase. This document describes the design decisions and remaining work.

### What's Done
- `WindowService` places windows on all exterior walls, producing both `GeometryData` (for Three.js) and `WindowElement` (for BIM export)
- `InteriorWallService` generates partition walls with door openings, producing both `GeometryData` and `DoorElement`
- `WallFaceService` generates perforated `ShapeGeometry` wall panels with cutouts for windows/doors
- IFC export creates full `IfcOpeningElement` -> `IfcRelVoidsElement` -> `IfcRelFillsElement` chains for both windows and doors
- Frontend renders glass with `MeshPhysicalMaterial` (transmission 0.9, opacity 0.3)
- XZ bounds filtering strips windows/walls outside compound footprints (L-shape, angled, split-level)

### What's Not Done
- Bay windows, skylights, garage doors
- User-controlled window/door placement
- Window shutters or detailed hardware

---

## Design Specs

### Doors
- **Main entrance:** 7ft tall x 3ft wide, ground floor, centered on front wall
- **Interior doors:** Openings in partition walls connecting rooms
- Door elements carry explicit (X, Z) position for IFC host wall matching (2ft tolerance)

### Windows
- Distributed across all exterior walls of all sections and floors
- Size varies by style: Modern = large (30% wall area), Victorian = medium (20%), Brutalist = small (10%)
- Default constants in `Constants/ArchitecturalConstants.cs` (window area ~12 sq ft)
- Sill height: 0.9m from floor level per story

### Style Variations (Implemented)
- **Modern:** Large windows, flat roof, white exterior, 30% glass ratio
- **Victorian:** Medium windows, gabled roof, cream exterior, 20% glass ratio
- **Brutalist:** Small windows, flat roof, gray exterior, 10% glass ratio

---

## Key Files

| File | Role |
|------|------|
| `Services/WindowService.cs` | Window placement + GeometryData + WindowElement generation |
| `Services/InteriorWallService.cs` | Partition walls + door openings + DoorElement generation |
| `Services/WallFaceService.cs` | Perforated wall panels (ShapeGeometry with cutouts) |
| `Geometry/WindowElement.cs` | Semantic window data for BIM export |
| `Geometry/DoorElement.cs` | Semantic door data for BIM export |
| `Geometry/Opening.cs` | Opening model |
| `Export/IfcExporter.cs` | IFC wall-opening-filling chains |
| `renderers/GeometryRenderer.js` | Frontend: glass material, wall face rendering |

---

## Architecture Notes

The **dual-output pattern** is critical here: `WindowService` and `InteriorWallService` each produce two parallel outputs:
1. `GeometryData` (flat vertex/index arrays) consumed by Three.js for rendering
2. Typed elements (`WindowElement`, `DoorElement`) consumed by `IfcExporter` for BIM

Don't collapse these into one form — they serve different consumers with different data needs.
