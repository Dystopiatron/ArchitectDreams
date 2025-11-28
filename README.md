# Architectural Dream Machine

THIS IS A WIP/NOT FINALIZED!!



Turn your ideas into 3D house designs instantly. Type a style, see a rotating 3D model, download for professional CAD software. 

---

## ⚡ Quick Start

**Terminal 1 - Start Backend:**
```bash
cd ArchitecturalDreamMachineBackend/ArchitecturalDreamMachineBackend
dotnet run
```

**Terminal 2 - Start Frontend:**
```bash
cd ArchitecturalDreamMachineFrontend
npx expo start
```

**Press 'w'** to open in your web browser → http://localhost:8081

**That's it!** 🎉

---

## 📚 Documentation

**New Users:**
- **[QUICKSTART.md](QUICKSTART.md)** - Get running in 2 minutes
- **[USER_GUIDE.md](USER_GUIDE.md)** - Complete walkthrough with examples

**Developers:**
- **[GETTING_STARTED.md](GETTING_STARTED.md)** - Technical setup and architecture
- **[BACKEND_API.md](ArchitecturalDreamMachineBackend/BACKEND_API.md)** - Backend API documentation
- **[FRONTEND_3D_CLIENT.md](ArchitecturalDreamMachineFrontend/FRONTEND_3D_CLIENT.md)** - Frontend 3D client
- **[HOUSE_LAYOUTS.md](HOUSE_LAYOUTS.md)** - Understanding the 5 layout types
- **[3D_FEATURES_GUIDE.md](3D_FEATURES_GUIDE.md)** - 3D viewer and OBJ export

---

## ✨ Features

- 🎨 **Natural Language Input** - "Modern glass house" → instant 3D model
- 🏗️ **3D Visualization** - Rotating house with proper roof, windows, door
- 📥 **OBJ Export** - Download for AutoCAD, Revit, Blender, SketchUp
- 💾 **Design History** - All designs saved to database
- 🎭 **3 Built-in Styles** - Modern, Victorian, Brutalist

---

## 🏛️ Architecture

```
React Native Web (Frontend)
         ↓ HTTP
ASP.NET Core API (Backend)
         ↓ Entity Framework
SQLite Database
```

**Tech Stack:**
- Backend: C# / ASP.NET Core 8.0 / Entity Framework / SQLite
- Frontend: React Native / Three.js / Expo
- 3D: Three.js 0.145.0 with WebGL rendering

---

## 🎯 Example Usage

1. Lot Size: `2500` sq ft
2. Style: `modern minimalist with large windows`
3. Click Generate → See 3D model
4. Download OBJ → Import to Blender/AutoCAD

**Style Keywords:**
- Modern: `modern`, `minimalist`, `glass`, `contemporary`
- Victorian: `victorian`, `ornate`, `classic`, `traditional`
- Brutalist: `brutalist`, `concrete`, `industrial`, `raw`

---

## 📋 Requirements

- macOS (or Windows/Linux with .NET 8)
- .NET 8 SDK
- Node.js 18+
- Web browser (Chrome/Firefox recommended)

---

## 🧪 Testing

**Backend tests:**
```bash
cd ArchitecturalDreamMachineBackend/ArchitecturalDreamMachineBackend
dotnet test
```

Tests cover: API validation, prompt parsing, geometry generation

---

## 🚀 API Endpoints

**Generate Design:**
```http
POST /api/designs/generate
Content-Type: application/json

{
  "lotSize": 2500,
  "stylePrompt": "modern minimalist"
}
```

**Export to OBJ:**
```http
GET /api/designs/{id}/export
```

**View API docs:** http://localhost:5095/swagger (when backend running)

---

## 🎓 For Developers

**Project Structure:**
```
ArchitectCode/
├── ArchitecturalDreamMachineBackend/  # C# API
│   ├── Controllers/                    # REST endpoints
│   ├── Data/                           # Database models
│   ├── Geometry/                       # 3D mesh generation
│   └── Tests/                          # xUnit tests
│
├── ArchitecturalDreamMachineFrontend/  # React Native
│   ├── screens/                        # UI screens
│   ├── components/                     # 3D viewer component
│   └── App.js                          # Navigation
│
└── Documentation/                      # User guides
```

**Add New Style:**
1. Edit `Data/AppDbContext.cs`
2. Add to `OnModelCreating` method
3. Restart backend (database auto-updates)

**Customize 3D:**
- Edit `components/HouseViewer3D.js`
- Modify geometry, materials, lighting
- Add windows, doors, details

---

## 📝 License

Educational/personal use

---

## 🙏 Credits

Built with:
- ASP.NET Core
- React Native & Expo
- Three.js
- Entity Framework Core

---

**Need help?** Check [USER_GUIDE.md](USER_GUIDE.md) for detailed instructions!
