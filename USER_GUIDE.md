# User Guide

A simple app that turns text prompts into 3D house designs. Type a style and lot size, see a rotating 3D model, download it for architectural software.

---

## Quick Start

See [QUICKSTART.md](QUICKSTART.md) for the 2-minute setup. Once running:

1. Lot size: `2500` (square feet)
2. Style: `modern glass house` or `victorian` or `brutalist`
3. Click **Generate Design**
4. The 3D house appears and rotates in your browser
5. Click **Download** for OBJ, IFC, or GLB files

---

## Style Guide

### Modern
Keywords: `modern`, `minimalist`, `contemporary`, `glass`
Flat roof, large windows (30%), white exterior.

### Victorian
Keywords: `victorian`, `ornate`, `classic`, `traditional`
Gabled roof, medium windows (20%), cream exterior.

### Brutalist
Keywords: `brutalist`, `concrete`, `industrial`, `raw`
Flat roof, small windows (10%), gray exterior.

The app extracts keywords from your prompt, ignoring common words ("with", "and", "the") and punctuation.

---

## The 3D View

**Building:** Size scales from lot size (2500 sq ft = 50ft x 50ft base). Color matches style. See [HOUSE_LAYOUTS.md](HOUSE_LAYOUTS.md) for the 5 layout types and how to pick one.

**Roof:** Gabled (Victorian) or flat (Modern, Brutalist). Multi-section layouts get separate roof sections per wing.

**Windows and doors:** Multiple windows on all exterior walls, sized by style. Interior partition walls have door openings.

**Controls:** Click-drag to rotate, scroll to zoom, preset view buttons (Top, Front, Side, Perspective).

---

## Downloads

Three export formats — see [3D_AND_EXPORT_GUIDE.md](3D_AND_EXPORT_GUIDE.md) for details:

- **OBJ** — raw mesh for Blender, AutoCAD, SketchUp
- **IFC4** — BIM data for Revit, ArchiCAD (includes walls, windows, doors, rooms)
- **GLB** — materials + glass for web viewers and Blender

---

## Troubleshooting

**Backend won't connect** — Make sure the backend terminal shows `Now listening on: http://localhost:5095`. Try http://localhost:5095/swagger in your browser.

**3D model not showing** — Use a web browser (press `w` in Expo). Try Chrome or Firefox. Make sure you clicked Generate first.

**Expo opens on phone instead of browser** — Use `npx expo start` then press `w`, not `npx expo start --ios`.

**Cache issues** — Run `npx expo start --clear`.

---

## Adding Styles

Developers can add new style templates in `Data/AppDbContext.cs` seed data, then delete `architecturaldreammachine.db` and restart.
