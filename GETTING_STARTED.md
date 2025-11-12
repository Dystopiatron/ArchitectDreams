# Architectural Dream Machine - Getting Started Guide

This guide will help you set up and run the complete Architectural Dream Machine application on macOS.

## Prerequisites

Before you begin, ensure you have:

1. **macOS** (Ventura or later recommended)
2. **.NET 8 SDK** - Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download)
3. **Node.js 18+** - Download from [nodejs.org](https://nodejs.org/)
4. **Xcode** - Install from Mac App Store (for iOS Simulator)
5. **Visual Studio Code** - Download from [code.visualstudio.com](https://code.visualstudio.com/)

### Verify Prerequisites

```bash
dotnet --version  # Should show 8.0.x or 9.0.x
node --version    # Should show v18.x or higher
```

## Step-by-Step Setup

### 1. Navigate to Project Directory

```bash
cd /Users/groundcontrol/Desktop/ArchitectCode
```

### 2. Backend Setup

```bash
# Navigate to backend
cd ArchitecturalDreamMachineBackend/ArchitecturalDreamMachineBackend

# Restore NuGet packages
dotnet restore

# Build the project
dotnet build

# Run tests to verify everything works
dotnet test

# Start the API server
dotnet run
```

**Expected Output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5162
```

Keep this terminal open - the backend must be running for the frontend to work.

### 3. Frontend Setup (New Terminal Window)

```bash
# Navigate to frontend directory
cd /Users/groundcontrol/Desktop/ArchitectCode/ArchitecturalDreamMachineFrontend

# Install dependencies (first time only)
npm install

# Start Expo development server
npx expo start

# Then choose how to run:
# Press 'i' → iOS Simulator (requires Xcode)
# Press 'w' → Web browser (runs on macOS in browser)
# Press 'a' → Android Emulator (if you have Android Studio)

# DO NOT use --ios flag as it may try to open on physical device
```

This will:
1. Start the Metro bundler
2. Show options to run on iOS Simulator, Web, or Android
3. Press the appropriate key for your platform

**Recommended for macOS:** Press `w` to run in your web browser (easiest option!)
**Alternative:** Press `i` to run in iOS Simulator (requires Xcode)

## Using the Application

### Backend API

1. **Swagger Documentation**: Visit http://localhost:5162/swagger
2. **Test Endpoint**: Try GET http://localhost:5162/api/designs

### Frontend App

You can run the app in multiple ways:

**Option 1: Web Browser (Easiest for macOS)**
- After running `npx expo start`, press `w`
- The app will open in your default web browser at http://localhost:8081
- Works great on macOS without needing Xcode!

**Option 2: iOS Simulator (Requires Xcode)**
- After running `npx expo start`, press `i`
- Opens in iOS Simulator
- More native iOS experience

**Option 3: Physical Device**
- Scan the QR code with Expo Go app
- See "Testing on Physical iOS Device" section below

### Using the App (any platform):

1. Enter a lot size (e.g., `2500`)
2. Enter a style prompt (e.g., `Modern minimalist design`)
3. Tap "Generate Design"
4. View the design parameters

### Example Prompts

- "Modern minimalist design with large windows"
- "Victorian style with ornate details"
- "Brutalist architecture, raw concrete"
- "Contemporary flat roof design"

## Testing on Physical iOS Device

If you want to test on your iPhone:

1. **Find your Mac's local IP address:**
   ```bash
   ifconfig | grep "inet " | grep -v 127.0.0.1
   ```
   Example output: `inet 192.168.1.100`

2. **Update the API URL:**
   - Open `ArchitecturalDreamMachineFrontend/screens/MainScreen.js`
   - Change line 13:
     ```javascript
     const API_BASE_URL = 'http://192.168.1.100:5162'; // Use your Mac's IP
     ```

3. **Run on device:**
   - Make sure your iPhone and Mac are on the same WiFi network
   - Scan the QR code from Expo with your iPhone camera
   - Open in Expo Go app

## Running Tests

### Backend Tests (xUnit)

```bash
cd ArchitecturalDreamMachineBackend/ArchitecturalDreamMachineBackend
dotnet test --verbosity normal
```

**Tests include:**
- Controller validation and error handling
- Prompt parser keyword extraction
- Geometry mesh generation
- Database operations

### Frontend Tests (Jest)

```bash
cd ArchitecturalDreamMachineFrontend
npm test
```

## Troubleshooting

### Backend Issues

**Problem:** Port 5162 already in use

**Solution:**
```bash
# Find process using port 5162
lsof -ti:5162 | xargs kill -9

# Or change port in Properties/launchSettings.json
```

**Problem:** Database errors

**Solution:**
```bash
# Delete existing database and restart
rm architecturaldreammachine.db
dotnet run
```

### Frontend Issues

**Problem:** Expo tries to open on physical device instead of simulator

**Solution:**
```bash
# Start Expo without auto-opening
npx expo start

# Then manually press 'i' in the terminal to open iOS Simulator
# Make sure Xcode and iOS Simulator are installed
```

**Problem:** "Cannot connect to backend"

**Solution:**
1. Verify backend is running (check http://localhost:5162/swagger)
2. Check API_BASE_URL in MainScreen.js
3. For physical device, use Mac's IP instead of localhost

**Problem:** React Native package version warnings

**Solution:**
```bash
# Install the recommended versions
cd ArchitecturalDreamMachineFrontend
npm install react-native-screens@~4.16.0
```

**Problem:** Metro bundler errors

**Solution:**
```bash
# Clear Expo cache
npx expo start --clear
```

**Problem:** App crashes or shows errors

**Solution:**
```bash
# Reinstall dependencies
rm -rf node_modules
npm install
npx expo start --clear
```

## Quick Start Script

For convenience, use the provided start script:

```bash
cd /Users/groundcontrol/Desktop/ArchitectCode
./start.sh
```

This will:
- Check prerequisites
- Build and start the backend
- Provide instructions for starting the frontend

## API Endpoints Reference

### POST /api/designs/generate

Generate a new design.

**Request:**
```json
{
  "lotSize": 2500,
  "stylePrompt": "Modern minimalist"
}
```

**Response:**
```json
{
  "houseParameters": {
    "lotSize": 2500,
    "roofType": "flat",
    "windowStyle": "large",
    "roomCount": 5,
    "material": {
      "color": "white",
      "texture": "glass"
    }
  },
  "mesh": { ... },
  "designId": 1,
  "styleName": "Modern"
}
```

### GET /api/designs

Get all designs.

**Response:**
```json
[
  {
    "id": 1,
    "lotSize": 2500,
    "styleKeywords": "modern, minimalist",
    "createdAt": "2025-11-11T..."
  }
]
```

## Architecture Overview

```
┌─────────────────────────────────────────────┐
│         React Native Frontend (iOS)         │
│    - Three.js 3D Rendering                  │
│    - User Input & Validation                │
│    - API Communication (axios)              │
└──────────────────┬──────────────────────────┘
                   │ HTTP/JSON
                   │
┌──────────────────▼──────────────────────────┐
│      ASP.NET Core Web API (Backend)         │
│    - RESTful Endpoints                      │
│    - Prompt Parsing                         │
│    - Design Generation                      │
│    - Geometry Calculation                   │
└──────────────────┬──────────────────────────┘
                   │
┌──────────────────▼──────────────────────────┐
│       SQLite Database                       │
│    - Designs                                │
│    - Style Templates                        │
└─────────────────────────────────────────────┘
```

## Next Steps

1. **Explore the code:**
   - Backend: `Controllers/DesignsController.cs`
   - Frontend: `screens/MainScreen.js`
   - Tests: `Tests/` directories

2. **Customize styles:**
   - Edit `Data/AppDbContext.cs` to add new style templates
   - Update seed data with your own architectural styles

3. **Enhance 3D rendering:**
   - Modify `Geometry/HouseParameters.cs` for complex shapes
   - Add roof geometry, windows, doors in frontend

4. **Implement stretch goals:**
   - Integrate Hugging Face for advanced NLP
   - Add OBJ export functionality
   - Implement texture mapping

## Support

For issues or questions:
1. Check the README files in each project directory
2. Review test files for usage examples
3. Inspect browser console (frontend) or terminal output (backend)

## Development Tips

- Use Visual Studio Code with C# and React Native extensions
- Enable Hot Reload in Expo for faster development
- Use Swagger UI for testing API endpoints
- Check backend logs for debugging API issues
- Use React DevTools for frontend debugging

Happy building! 🏗️✨
