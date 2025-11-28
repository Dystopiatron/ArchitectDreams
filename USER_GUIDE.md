# Architectural Dream Machine - Complete User Guide

A simple app that turns your ideas into 3D house designs. Type what you want, see it in 3D, download it for real architectural software.

---

## What Does This App Do?

1. **You type:** "Modern house with large windows" and a lot size
2. **App generates:** A 3D model with the right style, roof, windows, and materials
3. **You see:** A rotating 3D house you can view in your browser
4. **You download:** Professional 3D files (OBJ format) for AutoCAD, Revit, or Blender

---

## Quick Start

See **[QUICKSTART.md](QUICKSTART.md)** for the 2-minute setup guide.

Once running, create your first design:
1. Lot size: `2500` (square feet)
2. Style: `modern glass house` or `victorian` or `brutalist`
3. Click **"Generate Design"**
4. Watch your 3D house appear and rotate
5. Click **"Download OBJ File"** for use in AutoCAD/Blender/Revit

---

## Style Guide - What Can You Type?

### Built-in Styles

**Modern:**
- Keywords: `modern`, `minimalist`, `contemporary`, `glass`
- Results in: Flat roof, large windows, white color
- Example: "Modern minimalist design"

**Victorian:**
- Keywords: `victorian`, `ornate`, `classic`, `traditional`
- Results in: Peaked (gabled) roof, ornate windows, cream color
- Example: "Victorian style house"

**Brutalist:**
- Keywords: `brutalist`, `concrete`, `industrial`, `raw`
- Results in: Flat roof, small windows, gray concrete
- Example: "Brutalist concrete building"

### How the App Understands You

The app looks for keywords in what you type:
- Ignores: "with", "and", "the", "a"
- Removes: punctuation like commas and periods
- Matches: your keywords to style templates

**Examples:**
- "Modern house with large windows" → Finds **Modern**
- "Classic Victorian design" → Finds **Victorian**
- "Raw concrete brutalist architecture" → Finds **Brutalist**
- "Sleek contemporary glass" → Finds **Modern**

---

## Understanding the 3D View

### What You See

**The House:**
- **Size:** Based on your lot size (2500 sq ft = 50ft × 50ft base)
- **Layout:** Automatically varied! See 5 different architectural styles:
  - **Traditional Cube** (lot size ends in 0 or 5)
  - **Two-Story** (lot size ends in 1 or 6)
  - **L-Shaped** (lot size ends in 2 or 7)
  - **Split-Level** (lot size ends in 3 or 8)
  - **Angled Modern** (lot size ends in 4 or 9)
- **Height:** Varies by layout (single-story vs two-story)
- **Color:** Matches the style (white/cream/gray)

**Example:** Try lot sizes 2500, 2501, 2502, 2503, 2504 to see all 5 layouts!

**The Roof:**
- **Gabled:** Triangular peaked roof (Victorian)
  - Adapts to layout (multiple roofs for L-shaped)
- **Flat:** Horizontal roof (Modern, Brutalist)
  - Separate sections for multi-wing designs

**Windows & Door:**
- **Multiple windows** distributed across all building sections
- **1 door** centered on front
- Size varies by style (large/medium/small)
- Windows automatically placed on each wing or floor

**The Ground:**
- Green plane representing the lot

### Controls

- **Automatic rotation:** The house slowly spins so you can see all sides
- **Scrolling:** Scroll normally to see design details below
- Currently view-only (mouse controls coming in future updates)

**Understanding Layouts:** See **[HOUSE_LAYOUTS.md](HOUSE_LAYOUTS.md)** for complete details on all 5 layout types and how to get specific designs.

---

## Downloading Your Design

The OBJ file export works with all major 3D software:
- **Blender** (free) - File → Import → Wavefront (.obj)
- **AutoCAD** - Insert → Import → Select OBJ
- **SketchUp** - File → Import → 3D Model
- **Revit** - Import to create detailed BIM models

See **[3D_FEATURES_GUIDE.md](3D_FEATURES_GUIDE.md)** for detailed import instructions.

---

## Troubleshooting

### "Cannot connect to backend"

**Problem:** The frontend can't reach the backend
**Solution:**
1. Make sure backend terminal is still running
2. Check for the message "Now listening on: http://localhost:5095"
3. Try visiting http://localhost:5095/swagger in your browser - should show API docs

### "Page won't scroll"

**Problem:** Can't scroll down to see design details
**Solution:** Refresh your browser (this was recently fixed!)

### "3D model not showing"

**Problem:** You don't see the rotating house
**Solution:**
1. Make sure you're using a web browser (press 'w' in Expo)
2. Try Chrome or Firefox (best WebGL support)
3. Check that you clicked "Generate Design" first

### "Download button doesn't work"

**Problem:** Clicking download does nothing
**Solution:**
1. Check browser isn't blocking downloads
2. Make sure backend is running
3. Try generating the design again

### "Expo tries to open on my iPhone"

**Problem:** Wrong device opens
**Solution:** 
- Don't use `npx expo start --ios`
- Use `npx expo start` then press **'w'** for web

## Technical Details

**Stack:**
- Backend: C# / ASP.NET Core 8.0 / SQLite
- Frontend: React Native / Three.js / Expo

**Dimensions:**
- 2500 sq ft = 50ft × 50ft base, 30ft tall
- Scaling is proportional
- Database: `architecturaldreammachine.db` (SQLite)

See **[GETTING_STARTED.md](GETTING_STARTED.md)** for architecture details.

## Common Questions

**Do I need programming knowledge?** No, just follow the Quick Start guide.

**Is this free?** Yes, completely free to use. Generated designs are yours.

**Can I add more styles?** Yes - developers can add styles in `AppDbContext.cs`.

**Does it work offline?** Yes, runs entirely on your local machine.

**Can multiple people use it?** Yes, one backend can serve multiple browsers.

## For Developers

Want to customize or extend? See **[GETTING_STARTED.md](GETTING_STARTED.md)** for:
- Project structure and architecture
- How to add new style templates
- Customizing 3D rendering
- Testing and debugging

## Troubleshooting

**Not working?** Try these steps:
1. Check both terminals are running (backend + frontend)
2. Refresh your browser
3. Clear cache: `npx expo start --clear`
4. Restart backend: Ctrl+C, then `dotnet run`

For detailed troubleshooting, see **[GETTING_STARTED.md](GETTING_STARTED.md)**.

---

**Ready to design!** Start backend, start frontend, press 'w', and create. 🏗️✨
