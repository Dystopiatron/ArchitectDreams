# Architectural Dream Machine - Complete User Guide

A simple app that turns your ideas into 3D house designs. Type what you want, see it in 3D, download it for real architectural software.

---

## What Does This App Do?

1. **You type:** "Modern house with large windows" and a lot size
2. **App generates:** A 3D model with the right style, roof, windows, and materials
3. **You see:** A rotating 3D house you can view in your browser
4. **You download:** Professional 3D files (OBJ format) for AutoCAD, Revit, or Blender

---

## Quick Start (5 Minutes)

### Step 1: Start the Backend (The Brain)

Open a terminal window:

```bash
cd /Users/groundcontrol/Desktop/ArchitectCode/ArchitecturalDreamMachineBackend/ArchitecturalDreamMachineBackend
dotnet run
```

**Wait for this message:** `Now listening on: http://localhost:5095`

Leave this window open - the backend must keep running!

### Step 2: Start the Frontend (The Face)

Open a **NEW** terminal window:

```bash
cd /Users/groundcontrol/Desktop/ArchitectCode/ArchitecturalDreamMachineFrontend
npx expo start
```

**Press 'w'** when you see the menu → Opens in your web browser

**That's it!** The app is now running at http://localhost:8081

### Step 3: Create Your First Design

1. Enter lot size: `2500` (this is square feet)
2. Enter style: `modern glass house` (or try `victorian` or `brutalist`)
3. Click **"Generate Design"**
4. Watch your 3D house appear and rotate!
5. Click **"Download OBJ File"** to get a file for professional software

---

## Understanding the Parts

### What is the Backend?
Think of it as the brain - it:
- Understands your style descriptions
- Calculates house dimensions
- Generates 3D geometry
- Saves your designs to a database

### What is the Frontend?
Think of it as the face - it:
- Shows you input boxes
- Displays the rotating 3D model
- Lets you download files
- Works in your web browser

### What is a Database?
A file on your computer (`architecturaldreammachine.db`) that remembers all the designs you've created.

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

### Layout Variety Details

Want to understand the different layouts better? See **[HOUSE_LAYOUTS.md](HOUSE_LAYOUTS.md)** for:
- Detailed description of each layout type
- Best use cases for each design
- How to get specific layouts consistently
- Style + Layout combinations

---

## Downloading Your Design

### What is an OBJ File?

An OBJ file is a universal 3D model format that works with:
- **AutoCAD** - Industry standard for blueprints
- **Revit** - Building Information Modeling (BIM)
- **Blender** - Free 3D modeling software
- **SketchUp** - Easy architectural design
- Almost all professional 3D software!

### How to Use the Downloaded File

**In Blender (Free):**
1. Download from blender.org
2. Open Blender
3. File → Import → Wavefront (.obj)
4. Select your downloaded file
5. Add details, textures, render images

**In AutoCAD:**
1. Open AutoCAD
2. Insert → Import
3. Select the OBJ file
4. Create technical blueprints

**In SketchUp:**
1. Open SketchUp
2. File → Import → 3D Model
3. Modify and detail your design

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

---

## Technical Details (For the Curious)

### How Big is the House?

- **Lot size 2500 sq ft** = 50ft × 50ft base
- **Height** = 30ft (60% of base)
- **With gabled roof** = 40ft total height
- All measurements scale proportionally!

### What Languages/Tools?

**Backend:**
- C# programming language
- ASP.NET Core framework
- SQLite database
- .NET 8

**Frontend:**
- JavaScript programming language
- React Native framework
- Three.js for 3D graphics
- Expo development tools

### Where is Data Stored?

- Database file: `architecturaldreammachine.db`
- Location: Same folder as the backend
- Contains: Your designs and 3 style templates
- Can be deleted to start fresh

### Can I Run This on My Phone?

Currently optimized for web browser on computer. Mobile app features coming in future updates!

---

## Next Steps

### For Casual Users
- Try different style combinations
- Experiment with lot sizes
- Download OBJ files and explore in Blender (free)

### For Developers
- Add new style templates in `AppDbContext.cs`
- Customize 3D rendering in `HouseViewer3D.js`
- Add new material textures
- Implement advanced roof types

### Future Features (Planned)
- Mouse controls to rotate 3D view manually
- More style templates
- Interior room layouts
- Floor plan generation
- PDF blueprint export
- Better AI for understanding style descriptions

---

## Common Questions

**Q: Do I need to know programming?**
A: No! Just follow the Quick Start section.

**Q: Is this free?**
A: Yes, completely free to use.

**Q: Can I use the designs commercially?**
A: Yes, generated designs are yours to use.

**Q: How do I add more styles?**
A: Currently requires editing code - easier method coming soon!

**Q: Does it work offline?**
A: Backend and frontend must run on your computer, but no internet needed.

**Q: Can multiple people use it at once?**
A: One backend can serve multiple browsers - share the link!

---

## Getting Help

If something isn't working:

1. **Check both terminals are running** (backend + frontend)
2. **Try refreshing your browser**
3. **Clear Expo cache:** `npx expo start --clear`
4. **Restart backend:** Stop with Ctrl+C, then `dotnet run` again
5. **Check the detailed guides:** See GETTING_STARTED.md for technical details

---

**Ready to design your dream house?** 🏗️✨

Just remember: Backend first, frontend second, press 'w', and start creating!
