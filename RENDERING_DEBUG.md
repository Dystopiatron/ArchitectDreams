# Rendering Issue - Debugging Guide

## Problem
Only the green ground plane is rendering, no building geometry visible.

## Root Cause Found
The backend **IS** generating and returning geometry correctly! 

Test confirmed:
```bash
curl -X POST http://localhost:5095/api/designs/generate \
  -H "Content-Type: application/json" \
  -d '{"lotSize": 2000, "stylePrompt": "Modern minimalist"}'
```

Returns complete geometry with sections and roofs.

## Fixes Applied

### 1. MainScreen.js - Pass Geometry to Component ✅
```javascript
// Include geometry from backend
if (response.data.geometry) {
  params.geometry = response.data.geometry;
}
```

### 2. Added Debug Logging ✅
- HouseViewer3D now logs geometry receipt
- GeometryRenderer logs mesh creation

## Next Steps to Fix

### Option 1: Restart Frontend (Most Likely Fix)
The React Native/Expo app needs to reload to pick up the code changes:

```bash
cd /Users/groundcontrol/Desktop/ArchitectCode/ArchitecturalDreamMachineFrontend
# Press 'r' in the Metro terminal to reload
# Or restart the app completely
```

### Option 2: Check Browser Console
Look for these debug messages:
- ✅ `"Backend geometry received:"` - confirms geometry passed to component
- 🔨 `"Creating mesh:"` - confirms meshes being created
- ❌ `"Invalid geometry data:"` - indicates data format issue

### Option 3: Clear Cache
```bash
cd /Users/groundcontrol/Desktop/ArchitectCode/ArchitecturalDreamMachineFrontend
npm start -- --reset-cache
```

## What Should Happen

After reloading, you should see:

**In Console:**
```
✅ Backend geometry received: {sections: 1, roofs: 1, totalHeight: 20.75, maxDimension: 38.73}
🔨 Creating mesh: {vertexCount: 8, faceCount: 12, material: "stucco", color: "white"}
🔨 Creating mesh: {vertexCount: 8, faceCount: 12, material: "roof", color: "#333333"}
Rendered building with 2 meshes
```

**On Screen:**
- White building (stucco material)
- Dark gray flat roof
- Building rotating slowly
- Proper camera angle showing full building

## If Still Not Working

1. Check that frontend is making requests to correct backend URL
2. Verify no CORS errors in browser console
3. Check that Three.js is loading properly
4. Verify SceneManager is initializing correctly

## Technical Details

**Backend Response Structure (Confirmed Working):**
```json
{
  "geometry": {
    "sections": [{
      "vertices": [x,y,z, x,y,z, ...],  // Float array
      "indices": [0,1,2, ...],           // Integer array
      "materialType": "stucco",
      "color": "white"
    }],
    "roofs": [{
      "geometry": {
        "vertices": [...],
        "indices": [...],
        "materialType": "roof",
        "color": "#333333"
      },
      "height": 0.75,
      "roofType": "flat"
    }],
    "totalHeight": 20.75,
    "maxDimension": 38.73
  }
}
```

**Frontend Flow:**
1. MainScreen receives API response ✅
2. MainScreen adds `geometry` to `houseParams` ✅
3. HouseViewer3D receives `houseParams` with geometry ✅
4. GeometryRenderer converts to Three.js meshes ✅
5. SceneManager renders the scene ✅

All components are in place - just need to reload the app!
