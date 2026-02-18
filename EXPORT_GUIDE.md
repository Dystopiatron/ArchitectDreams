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

- Building footprint scaled from lot size (e.g. 2500 sq ft → 50 ft × 50 ft base)
- Roof type from style (flat for Modern/Brutalist, gabled for Victorian)
- Windows on all exterior walls (count and size based on style)
- Interior partition walls with door openings
- Style-specific colors (Modern: white, Victorian: cream, Brutalist: gray)

### Browser Compatibility

Works in Chrome, Firefox, Safari, Edge. The 3D viewer requires WebGL — use a desktop browser, not a mobile app.

---

## Export Formats

Three download formats are available from the **Download** button or directly via the API.

### OBJ — Mesh Export

Best for 3D modeling software that needs raw geometry.

```http
GET /api/designs/{id}/export
→ house_design_{id}_{style}.obj
```

**Import into:**

| Software | Steps |
|---|---|
| Blender (free) | File → Import → Wavefront (.obj) |
| AutoCAD | Insert → Import → Select OBJ file |
| SketchUp | File → Import → 3D Model (.obj) |
| Revit | Use IFC export instead (better fidelity) |

**OBJ file contains:** vertex coordinates, surface normals, face definitions. No material or semantic data.

---

### IFC4 — BIM Export

Best for architectural and engineering workflows. Preserves semantic data (rooms, walls, windows, roof type).

```http
GET /api/designs/{id}/export/ifc
→ design_{id}.ifc
```

**Import into:**

| Software | Steps |
|---|---|
| Revit | File → Open → IFC |
| ArchiCAD | File → Open → IFC |
| BIM Vision (free viewer) | File → Open |
| Blender (Bonsai add-on) | File → Open |

**IFC file contains:** IfcProject hierarchy, IfcBuildingStorey per floor, IfcSpace per room, IfcWall, IfcWindow, IfcRoof, property sets with architectural metadata.

---

### GLB — Binary glTF Export

Best for web viewers, game engines, and Blender with full material support.

```http
GET /api/designs/{id}/export/gltf
→ design_{id}.glb
```

**Open with:**

| Tool | Notes |
|---|---|
| Blender | File → Import → glTF 2.0 |
| Three.js (GLTFLoader) | Native support |
| Online: gltf-viewer.donmccurdy.com | No install needed |
| Windows 3D Viewer | Double-click the file |

**GLB file contains:** PBR materials, transparent glass for large-window styles, style-specific colors. Smaller and faster to load than OBJ for web use.

---

## Troubleshooting

**3D model not showing**
- Make sure you opened in a web browser (press `w` in Expo, not `i` or `a`)
- Confirm backend is running and shows `Now listening on: http://localhost:5095`
- Try Chrome or Firefox if another browser fails

**Download not working**
- Allow file downloads in your browser settings
- Confirm the design generated successfully (no error message shown)
- Check backend terminal for error output

**Import looks wrong in Blender/AutoCAD**
- For BIM workflows, prefer the IFC export over OBJ
- For web/visual work, prefer the GLB export over OBJ

---

See [USER_GUIDE.md](USER_GUIDE.md) for complete usage instructions.
