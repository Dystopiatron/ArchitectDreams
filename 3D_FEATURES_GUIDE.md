# 3D Features Guide

## What You Get

### 1. Interactive 3D Viewer
- **Rotating 3D model** of your house design
- **Accurate geometry** - roof, windows, door, correct colors
- **Runs in web browser** - no special software needed

### 2. OBJ File Export
- **Download button** - saves 3D file to your computer
- **Works with professional software:**
  - AutoCAD (blueprints)
  - Revit (building design)
  - Blender (free 3D modeling)
  - SketchUp (easy design)

---

## How to Use

### Generate and View

1. Start backend: `dotnet run` (port 5095)
2. Start frontend: `npx expo start` then press **'w'**
3. Enter lot size and style
4. Click **Generate Design**
5. See rotating 3D model at top of page
6. Click **📥 Download OBJ File** to save

### Import to Professional Software

**Blender (Free):**
```
File → Import → Wavefront (.obj)
```

**AutoCAD:**
```
Insert → Import → Select OBJ file
```

**SketchUp:**
```
File → Import → 3D Model (.obj)
```

---

## What the 3D Model Shows

### House Dimensions
- **Base size:** Square footprint from lot size
  - Example: 2500 sq ft = 50ft × 50ft
- **Height:** 60% of base (looks proportional)
  - Example: 50ft base = 30ft tall

### Roof Types
- **Gabled:** Triangular peaked roof (Victorian style)
- **Flat:** Level top (Modern, Brutalist styles)

### Details
- **3 windows** on front wall (size varies by style)
- **1 door** centered at bottom
- **Ground plane** representing the lot
- **Colors** from style template:
  - Modern: White
  - Victorian: Cream
  - Brutalist: Gray

---

## Browser Requirements

**Works best in:**
- ✅ Chrome
- ✅ Firefox
- ✅ Safari
- ✅ Edge

**Note:** 3D viewer only works in **web browser**, not mobile apps (WebGL limitation)

---

## Troubleshooting

### 3D Model Not Showing
1. Press 'w' to use web browser
2. Check backend is running (port 5095)
3. Try Chrome or Firefox
4. Check browser console for errors

### Download Not Working
1. Allow browser downloads
2. Verify design generated successfully
3. Check backend terminal for errors

### Can't Scroll Page
1. Refresh browser
2. This was recently fixed - make sure you have latest code

---

## OBJ File Format

The downloaded file contains:

```obj
# Comments with design info
# Lot Size: 2500 sq ft
# Style: Modern

# 3D coordinates
v -25.0 0.0 -25.0
v 25.0 0.0 -25.0
...

# Surface normals
vn 0.0 1.0 0.0
...

# Faces (triangles)
f 1//1 2//1 3//1
...

# Roof geometry
# (varies by roof type)
```

This is a **standard format** recognized by all major 3D software.

---

## Future Enhancements

Ideas for the future:
- Mouse controls to rotate manually
- Better materials and textures
- Interior rooms and walls
- Different roof styles
- Floor plan view
- PDF blueprint export

---

## API Endpoint

**Export Design:**
```http
GET /api/designs/{id}/export

Response: house_design_{id}_{style}.obj file
```

**Example:**
```bash
curl http://localhost:5095/api/designs/2/export -o house.obj
```

---

**Ready to create 3D architectural models!** 🏗️✨

See [USER_GUIDE.md](USER_GUIDE.md) for complete instructions.
