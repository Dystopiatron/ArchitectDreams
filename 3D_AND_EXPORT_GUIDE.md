# Export Guide

How to view, interact with, and export your 3D architectural designs.

---

## 3D Viewer

After generating a design:

1. Start the backend: `dotnet run` (port 5095)
2. Start the frontend: `npx expo start` then press **`w`**
3. Enter lot size and style, click **Generate Design**
4. The rotating 3D model appears at the top of the page

### Camera Controls

- **Rotate** — click and drag
- **Zoom** — scroll wheel
- **Preset views** — Top, Front, Side, Perspective buttons

### What the Model Shows

- Building footprint scaled from lot size (e.g. 2500 sq ft -> 50 ft x 50 ft base)
- Roof type from style (flat for Modern/Brutalist, gabled for Victorian)
- Windows on all exterior walls (count and size based on style)
- Interior partition walls with door openings
- Style-specific colors (Modern: white, Victorian: cream, Brutalist: gray)

Requires WebGL — use a desktop browser (Chrome, Firefox, Safari, Edge), not a mobile app.

---

## Export Formats

Three download formats available from the **Download** button or directly via the API.

### OBJ — Mesh Export

Raw geometry for 3D modeling software. No material or semantic data.

```
GET /api/designs/{id}/export
```

Import via File > Import > Wavefront (.obj) in Blender, SketchUp, AutoCAD. For BIM workflows (Revit), use IFC instead.

---

### IFC4 — BIM Export

Semantic BIM data for architectural/engineering workflows. Preserves rooms, walls, windows, roof type.

```
GET /api/designs/{id}/export/ifc
```

Contains: IfcProject hierarchy, IfcBuildingStorey per floor, IfcSpace per room, IfcWallStandardCase, IfcWindow (with openings), IfcDoor, IfcRoof, IfcSlab, property sets.

Opens in Revit, ArchiCAD, BIM Vision, Blender (Bonsai add-on).

---

### GLB — Binary glTF Export

Full PBR materials, transparent glass, style-specific colors. Smaller and faster than OBJ for web use.

```
GET /api/designs/{id}/export/gltf
```

Opens in Blender, Three.js (GLTFLoader), Windows 3D Viewer, or online at gltf-viewer.donmccurdy.com.

---

## Troubleshooting

**3D model not showing** — Make sure you opened in a web browser (press `w` in Expo, not `i` or `a`). Confirm backend is running on port 5095.

**Download not working** — Allow file downloads in your browser. Check backend terminal for errors.

**Import looks wrong** — Use IFC for BIM workflows (Revit), GLB for web/visual work. OBJ is bare geometry only.
