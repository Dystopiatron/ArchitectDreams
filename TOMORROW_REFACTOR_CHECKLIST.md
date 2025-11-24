# Refactoring Checklist - November 25, 2025

## Goal: Backend Does Heavy Lifting, Frontend Just Renders

**Philosophy:** Backend calculates ALL geometry (vertices, faces, dimensions). Frontend receives ready-to-render data and uses Three.js to display it.

---

## Morning Session (4 hours) - Backend Heavy Lifting

### ☐ Step 1: Backend Geometry Foundation (1.5 hours)

**Create these files:**

1. **`Models/GeometryData.cs`** - Data transfer objects
   ```csharp
   public class GeometryData
   {
       public float[] Vertices { get; set; }  // [x,y,z, x,y,z, ...]
       public int[] Indices { get; set; }      // Triangle faces
       public string MaterialType { get; set; }
       public string Color { get; set; }
   }
   
   public class BuildingGeometry
   {
       public List<GeometryData> Sections { get; set; }
       public GeometryData Roof { get; set; }
       public List<GeometryData> Windows { get; set; }
       public List<GeometryData> InteriorWalls { get; set; }
       public double TotalHeight { get; set; }
       public double MaxDimension { get; set; }
   }
   ```

2. **`Geometry/VertexCalculator.cs`** - Calculate vertices
   ```csharp
   public static class VertexCalculator
   {
       public static float[] CalculateBoxVertices(double width, double height, double depth)
       {
           // Returns 8 vertices for box
       }
       
       public static float[] CalculateGabledRoofVertices(
           double width, double depth, double height, double overhang)
       {
           // Returns 6 vertices: 2 ridge + 4 eaves
           // PORT FROM Phase 1.1 fix
       }
   }
   ```

3. **`Geometry/FaceGenerator.cs`** - Generate face indices
   ```csharp
   public static class FaceGenerator
   {
       public static int[] BoxFaces() 
       {
           // Returns 36 indices (12 triangles, 6 faces)
       }
       
       public static int[] GabledRoofFaces()
       {
           // Returns 24 indices (8 triangles)
           // PORT FROM Phase 1.1 fix
       }
   }
   ```

4. **`Services/GeometryService.cs`** - Main geometry orchestrator
   ```csharp
   public class GeometryService
   {
       public GeometryData CreateBox(double width, double height, double depth, 
           double x, double y, double z, string materialType, string color);
       
       public GeometryData CreateGabledRoof(double width, double depth, 
           double roofPitch, double overhang);
       
       public GeometryData CreateFlatRoof(double width, double depth, double overhang);
   }
   ```

**Test:** Call `GeometryService.CreateGabledRoof(30, 40, 8.0, 1.5)`, verify:
- Returns 6 vertices (2 ridge + 4 eaves)
- Returns 24 indices (8 triangular faces)
- Height calculated correctly: `(width/2) * (8.0/12) = 10 ft`

---

### ☐ Step 2: Backend Layout Strategy (1.5 hours)

**Create these files:**

1. **`Models/LayoutData.cs`** - Layout DTOs
   ```csharp
   public class LayoutSection
   {
       public double Width { get; set; }
       public double Height { get; set; }
       public double Depth { get; set; }
       public double X { get; set; }
       public double Y { get; set; }
       public double Z { get; set; }
       public int Floor { get; set; }
   }
   
   public class LayoutData
   {
       public List<LayoutSection> Sections { get; set; }
       public List<RoofSection> RoofSections { get; set; }
       public double TotalWidth { get; set; }
       public double TotalDepth { get; set; }
       public double TotalHeight { get; set; }
   }
   
   public class RoofSection
   {
       public double Width { get; set; }
       public double Depth { get; set; }
       public double X { get; set; }
       public double Y { get; set; }
       public double Z { get; set; }
   }
   ```

2. **`LayoutStrategies/ILayoutStrategy.cs`** - Interface
   ```csharp
   public interface ILayoutStrategy
   {
       LayoutData CalculateLayout(
           double footprintWidth, 
           double footprintDepth, 
           double ceilingHeight, 
           int stories);
   }
   ```

3. **`LayoutStrategies/CubeLayoutStrategy.cs`**
   ```csharp
   public class CubeLayoutStrategy : ILayoutStrategy
   {
       public LayoutData CalculateLayout(...)
       {
           // Single rectangular section
           // Roof section matches building dimensions
       }
   }
   ```

4. **`LayoutStrategies/LShapeLayoutStrategy.cs`**
   ```csharp
   public class LShapeLayoutStrategy : ILayoutStrategy
   {
       public LayoutData CalculateLayout(...)
       {
           // Two perpendicular sections
           // Two roof sections (one per wing)
           // PORT FROM existing HouseViewer3D.js L-shape logic
       }
   }
   ```

5. **`Services/LayoutService.cs`**
   ```csharp
   public class LayoutService
   {
       public LayoutData DetermineLayout(
           string styleName, 
           string buildingShape, 
           double footprintWidth, 
           double footprintDepth, 
           double ceilingHeight, 
           int stories)
       {
           ILayoutStrategy strategy = buildingShape switch
           {
               "l-shape" => new LShapeLayoutStrategy(),
               "two-story" => new TwoStoryLayoutStrategy(),
               _ => new CubeLayoutStrategy()
           };
           
           return strategy.CalculateLayout(footprintWidth, footprintDepth, ceilingHeight, stories);
       }
   }
   ```

**Test:** Call `LayoutService.DetermineLayout("Victorian", "l-shape", 30, 40, 9, 2)`, verify:
- Returns 2 sections (main wing + side wing)
- Returns 2 roof sections
- Dimensions match expected L-shape proportions

---

### ☐ Step 3: Backend Roof Strategy (1 hour)

**Create these files:**

1. **`Models/RoofData.cs`**
   ```csharp
   public class RoofData
   {
       public GeometryData Geometry { get; set; }
       public double Height { get; set; }
       public string RoofType { get; set; }
       public double Pitch { get; set; }
   }
   ```

2. **`RoofStrategies/IRoofStrategy.cs`**
   ```csharp
   public interface IRoofStrategy
   {
       RoofData CalculateRoof(RoofSection section, double roofPitch, double overhang);
   }
   ```

3. **`RoofStrategies/GabledRoofStrategy.cs`**
   ```csharp
   public class GabledRoofStrategy : IRoofStrategy
   {
       public RoofData CalculateRoof(RoofSection section, double roofPitch, double overhang)
       {
           // Use GeometryService.CreateGabledRoof()
           // PORT Phase 1.1 calculation logic
           // Returns vertices, faces, height
       }
   }
   ```

4. **`RoofStrategies/FlatRoofStrategy.cs`**
   ```csharp
   public class FlatRoofStrategy : IRoofStrategy
   {
       public RoofData CalculateRoof(RoofSection section, double roofPitch, double overhang)
       {
           // Use GeometryService.CreateFlatRoof()
           // Includes parapet walls if specified
       }
   }
   ```

5. **`Services/RoofService.cs`**
   ```csharp
   public class RoofService
   {
       public List<RoofData> CalculateRoofs(
           List<RoofSection> sections, 
           string roofType, 
           double roofPitch, 
           double overhang, 
           bool hasParapet)
       {
           IRoofStrategy strategy = roofType.ToLower() switch
           {
               "gabled" => new GabledRoofStrategy(),
               "flat" => new FlatRoofStrategy(),
               _ => new FlatRoofStrategy()
           };
           
           return sections.Select(s => strategy.CalculateRoof(s, roofPitch, overhang)).ToList();
       }
   }
   ```

**Test:** Call `RoofService.CalculateRoofs(sections, "gabled", 8.0, 1.5, false)`, verify:
- Returns RoofData with geometry (6 vertices, 24 indices)
- Height matches Phase 1.1 calculation
- Overhang applied correctly

---

## Afternoon Session (3-4 hours) - Frontend Thin Client

### ☐ Step 4: Backend Orchestration (30 minutes)

**Update these files:**

1. **`Services/DesignOrchestrationService.cs`** (NEW)
   ```csharp
   public class DesignOrchestrationService
   {
       private readonly GeometryService _geometryService;
       private readonly LayoutService _layoutService;
       private readonly RoofService _roofService;
       
       public BuildingGeometry GenerateCompleteGeometry(HouseParameters parameters)
       {
           // 1. Get layout (sections, roof sections)
           var layout = _layoutService.DetermineLayout(...);
           
           // 2. Generate section geometries
           var sectionGeometries = layout.Sections.Select(s => 
               _geometryService.CreateBox(s.Width, s.Height, s.Depth, s.X, s.Y, s.Z, 
                   parameters.ExteriorMaterial, parameters.Material.Color)
           ).ToList();
           
           // 3. Generate roof geometries
           var roofGeometries = _roofService.CalculateRoofs(
               layout.RoofSections, 
               parameters.RoofType, 
               parameters.RoofPitch, 
               parameters.HasEaves ? parameters.EavesOverhang : 0,
               parameters.HasParapet
           );
           
           // 4. Return complete geometry
           return new BuildingGeometry
           {
               Sections = sectionGeometries,
               Roofs = roofGeometries,
               TotalHeight = layout.TotalHeight + roofGeometries.Max(r => r.Height),
               MaxDimension = Math.Max(layout.TotalWidth, layout.TotalDepth)
           };
       }
   }
   ```

2. **`Controllers/DesignsController.cs`** - Update `/generate` endpoint
   ```csharp
   [HttpPost("generate")]
   public async Task<ActionResult<object>> Generate([FromBody] GenerateRequest request)
   {
       // ... existing style parsing ...
       
       var houseParameters = await GenerateHouseParameters(request, styleTemplate);
       
       // NEW: Generate complete geometry on backend
       var geometry = _orchestrationService.GenerateCompleteGeometry(houseParameters);
       
       return Ok(new
       {
           houseParameters,
           geometry, // NEW: Complete geometry ready for Three.js
           designId = design.Id
       });
   }
   ```

**Test:** Call API `/api/designs/generate`, verify response includes:
- `geometry.sections[]` with vertices and indices
- `geometry.roofs[]` with vertices and indices
- `geometry.totalHeight`
- `geometry.maxDimension`

---

### ☐ Step 5: Frontend GeometryRenderer (45 minutes)

**Create these files:**

1. **`renderers/SceneManager.js`** - Three.js initialization
   ```javascript
   export class SceneManager {
     constructor(mountElement, width, height) {
       this.scene = new THREE.Scene();
       this.camera = new THREE.PerspectiveCamera(50, width / height, 0.1, 1000);
       this.renderer = new THREE.WebGLRenderer({ antialias: true });
       
       this._initScene();
       this._initLighting();
       this._initGround();
     }
     
     // ... (as detailed in REFACTORING_ARCHITECTURE_PLAN.md)
   }
   ```

2. **`renderers/GeometryRenderer.js`** - Convert backend data to Three.js
   ```javascript
   export class GeometryRenderer {
     static createMeshFromGeometry(geometryData) {
       const geometry = new THREE.BufferGeometry();
       
       // Set vertices
       geometry.setAttribute('position', 
         new THREE.BufferAttribute(new Float32Array(geometryData.vertices), 3)
       );
       
       // Set face indices
       geometry.setIndex(geometryData.indices);
       geometry.computeVertexNormals();
       
       // Create material
       const material = this.createMaterial(geometryData.materialType, geometryData.color);
       
       return new THREE.Mesh(geometry, material);
     }
     
     static createMaterial(materialType, color) {
       return new THREE.MeshStandardMaterial({
         color: new THREE.Color(color),
         roughness: materialType === 'concrete' ? 0.95 : 0.7,
         side: THREE.DoubleSide
       });
     }
     
     static renderBuilding(houseGroup, buildingGeometry) {
       // Add sections
       buildingGeometry.sections.forEach(sectionData => {
         const mesh = this.createMeshFromGeometry(sectionData);
         mesh.castShadow = true;
         mesh.receiveShadow = true;
         houseGroup.add(mesh);
       });
       
       // Add roofs
       buildingGeometry.roofs.forEach(roofData => {
         const mesh = this.createMeshFromGeometry(roofData.geometry);
         mesh.castShadow = true;
         houseGroup.add(mesh);
       });
     }
   }
   ```

3. **`utils/DisposalManager.js`**
   ```javascript
   export class DisposalManager {
     static dispose(scene) {
       scene.traverse((object) => {
         if (object.geometry) {
           object.geometry.dispose();
         }
         if (object.material) {
           if (Array.isArray(object.material)) {
             object.material.forEach(m => m.dispose());
           } else {
             object.material.dispose();
           }
         }
       });
     }
   }
   ```

**Test:** Create mesh from backend geometry data, verify:
- BufferGeometry created with correct vertices
- Faces indexed correctly
- Material applied
- Renders in Three.js scene

---

### ☐ Step 6: Refactor HouseViewer3D (45 minutes)

**Update `HouseViewer3D.js`:**

```javascript
import React, { useEffect, useRef } from 'react';
import { SceneManager } from './renderers/SceneManager.js';
import { GeometryRenderer } from './renderers/GeometryRenderer.js';
import { DisposalManager } from './utils/DisposalManager.js';
import * as THREE from 'three';

export default function HouseViewer3D({ houseParams }) {
  const mountRef = useRef(null);
  const sceneManagerRef = useRef(null);
  const houseGroupRef = useRef(null);
  
  useEffect(() => {
    if (!mountRef.current || !houseParams) {
      console.log('HouseViewer3D: Missing mount or params');
      return;
    }
    
    console.log('HouseViewer3D: Initializing with params', houseParams);
    
    let frameId;
    
    try {
      // Initialize scene
      const sceneManager = new SceneManager(
        mountRef.current,
        mountRef.current.clientWidth,
        400
      );
      sceneManagerRef.current = sceneManager;
      
      // Create house group
      const houseGroup = new THREE.Group();
      sceneManager.scene.add(houseGroup);
      houseGroupRef.current = houseGroup;
      
      // Render building from backend geometry
      if (houseParams.geometry) {
        GeometryRenderer.renderBuilding(houseGroup, houseParams.geometry);
      }
      
      // Position camera
      sceneManager.positionCamera(
        houseParams.geometry.maxDimension,
        houseParams.geometry.totalHeight
      );
      
      // Animation loop
      const animate = () => {
        frameId = requestAnimationFrame(animate);
        houseGroup.rotation.y += 0.003;
        sceneManager.render();
      };
      animate();
      
      // Resize handler
      const handleResize = () => {
        const width = mountRef.current.clientWidth;
        sceneManager.camera.aspect = width / 400;
        sceneManager.camera.updateProjectionMatrix();
        sceneManager.renderer.setSize(width, 400);
      };
      window.addEventListener('resize', handleResize);
      
      // Cleanup
      return () => {
        if (frameId) cancelAnimationFrame(frameId);
        window.removeEventListener('resize', handleResize);
        DisposalManager.dispose(sceneManager.scene);
        sceneManager.dispose();
      };
      
    } catch (error) {
      console.error('Error initializing HouseViewer3D:', error);
    }
  }, [houseParams]);
  
  return <div ref={mountRef} style={{ width: '100%', height: 400 }} />;
}
```

**Key Changes:**
- ❌ Removed ALL geometry calculation logic (was 600+ lines)
- ✅ Frontend only renders `houseParams.geometry` from backend
- ✅ Uses `GeometryRenderer` to convert backend data to Three.js
- ✅ Scene initialization delegated to `SceneManager`
- ✅ Memory cleanup delegated to `DisposalManager`

**Test:** Victorian, Modern, Brutalist all render correctly from backend geometry

---

### ☐ Step 7: Integration Testing (1 hour)

**Backend Tests:**
1. Test `GeometryService.CreateGabledRoof()` returns correct vertices
2. Test `LayoutService` returns correct sections for each layout type
3. Test `RoofService` calculates correct roof height
4. Test API `/generate` returns complete geometry

**Frontend Tests:**
1. Test `GeometryRenderer.createMeshFromGeometry()` creates valid Three.js mesh
2. Test `SceneManager` initializes scene, camera, lighting
3. Test `DisposalManager` cleans up Three.js resources

**Integration Tests:**
1. Generate Victorian design → verify gabled roof geometry
2. Generate Brutalist design → verify flat roof with parapet
3. Generate Modern design → verify flat roof
4. Compare visual output before/after refactor (manual verification)
5. Test memory usage stable after multiple regenerations

**Performance Benchmarks:**
- API response time < 100ms
- Frontend render time < 50ms
- Memory usage stable (no leaks)

---

### ☐ Step 8: Documentation (30 minutes)

**Update these files:**

1. **`API_DOCUMENTATION.md`** (NEW)
   - Document `/api/designs/generate` response schema
   - Include `geometry` object structure
   - Example request/response

2. **`BACKEND_ARCHITECTURE.md`** (NEW)
   - Sequence diagram: Controller → Services → Strategies → Response
   - How to add new roof type
   - How to add new layout type

3. **`FRONTEND_ARCHITECTURE.md`** (NEW)
   - Thin client philosophy
   - GeometryRenderer usage
   - How to add new rendering features

---

## Success Criteria

### ✅ Backend Heavy Lifting
- [ ] Backend calculates ALL geometry (vertices, faces, dimensions)
- [ ] API returns complete `BuildingGeometry` object
- [ ] No geometry calculations in frontend

### ✅ Frontend Thin Client
- [ ] Frontend only renders backend data
- [ ] HouseViewer3D < 200 lines (was 942)
- [ ] No conditional style logic in frontend

### ✅ Extensibility
- [ ] Add new roof type: backend only (1 new strategy class)
- [ ] Add new layout: backend only (1 new strategy class)
- [ ] Frontend requires ZERO changes

### ✅ Quality
- [ ] All existing designs render identically
- [ ] Memory cleanup working (no leaks)
- [ ] Phase 1.1 roof fix preserved in backend

---

## Tomorrow's Schedule (November 25, 2025)

**9:00 AM - 10:30 AM:** Backend Geometry Foundation (Step 1)  
**10:30 AM - 12:00 PM:** Backend Layout Strategy (Step 2)  
**12:00 PM - 1:00 PM:** Lunch Break  
**1:00 PM - 2:00 PM:** Backend Roof Strategy (Step 3)  
**2:00 PM - 2:30 PM:** Backend Orchestration (Step 4)  
**2:30 PM - 3:15 PM:** Frontend GeometryRenderer (Step 5)  
**3:15 PM - 4:00 PM:** Refactor HouseViewer3D (Step 6)  
**4:00 PM - 5:00 PM:** Integration Testing (Step 7)  
**5:00 PM - 5:30 PM:** Documentation (Step 8)

**Total:** 8 hours (with 1 hour lunch)

---

## Notes

- **Port Phase 1.1 Fix:** Gabled roof calculation from `HouseViewer3D.js` → `GabledRoofStrategy.cs`
- **Keep Phase 1.1 Tests:** `roofGeometry.test.js` still valid, update to test backend API
- **Feature Flag:** Not needed for Day 1 - test locally first
- **Testing Bridge:** Manual verification for visual parity, automated tests for geometry calculations

---

## Post-Refactor: Phase 1.2-1.4 Bug Fixes (Future)

After refactoring complete, these bugs become EASIER:

- **Phase 1.2 - Memory Disposal:** Already fixed with `DisposalManager`
- **Phase 1.3 - CORS Security:** Backend service layer separates concerns
- **Phase 1.4 - Story Override Warning:** Backend validation in `DesignOrchestrationService`

---

**Ready to rock tomorrow! 🚀**
