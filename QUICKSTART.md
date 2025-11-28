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
✅ Wait for: `Now listening on: http://localhost:5095`

### 2. Start Frontend (New Terminal)
```bash
cd ArchitecturalDreamMachineFrontend
npx expo start
```
✅ Press **'w'** to open in browser

### 3. Generate a Design
1. Lot size: `2500`
2. Style: `modern glass house`
3. Click **Generate Design**
4. Watch your 3D house rotate! 🏠

## Troubleshooting

**Backend won't start?**
- Verify .NET 8 SDK: `dotnet --version`
- Check port 5095 not in use

**Frontend won't load?**
- Clear cache: `npx expo start --clear`
- Try different browser (Chrome recommended)

**Can't connect?**
- Verify backend shows "Now listening on: http://localhost:5095"
- Visit http://localhost:5095/swagger to test

## What's Included

✅ 3 built-in architectural styles (Modern, Victorian, Brutalist)  
✅ 5 different building layouts (cube, L-shape, two-story, split-level, angled)  
✅ Rotating 3D models in browser  
✅ OBJ file export for AutoCAD/Blender/Revit  

**Next:** See [USER_GUIDE.md](USER_GUIDE.md) for detailed features and [HOUSE_LAYOUTS.md](HOUSE_LAYOUTS.md) to understand layout varieties.
