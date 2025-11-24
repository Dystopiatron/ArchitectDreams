# Architectural Refactoring Plan - Modular Design System

## Executive Summary

**Current State:** Monolithic architecture with 942-line `HouseViewer3D.js` and tightly coupled rendering logic. All architectural styles handled through conditional statements.

**Target State:** Modular, extensible architecture using Strategy pattern, Factory pattern, and separation of concerns. Each architectural style, roof type, and building component is independently developed and tested.

**Timeline:** Full day refactor session (November 25, 2025)
- Phase 1: Backend Refactoring (PRIORITY - 4 hours) - Backend does heavy lifting
- Phase 2: Frontend Refactoring (3-4 hours) - Frontend compiles data from backend
- Total: 7-8 hours

**Migration:** 5-week incremental rollout with feature flags

**Benefits:**
- ✅ Easy to add new architectural styles without touching existing code
- ✅ Bug fixes isolated to specific modules
- ✅ Testable components
- ✅ Reduced complexity per file (< 200 lines each)
- ✅ Clear separation of concerns
- ✅ Easier onboarding for new developers

---

## Current Architecture Analysis

### 1. Frontend Structure (React Native + Three.js)

**Single File:** `HouseViewer3D.js` (942 lines)

**Responsibilities (Violates Single Responsibility Principle):**
1. Scene setup (Three.js initialization, lighting, camera)
2. Layout determination (5 different layout types)
3. Building generation (walls, floors, foundations)
4. Roof generation (gabled vs flat, multiple layouts)
5. Window placement (3 different styles)
6. Door creation
7. Interior walls (room-based)
8. Material selection
9. Animation loop
10. Cleanup/disposal

**Key Functions:**
- `createHouseSection(width, height, depth, x, y, z, addWindows, floorNum)` - 95 lines
- `createInteriorWalls(rooms)` - 140 lines
- `createGabledRoof(width, depth)` - 60 lines
- Main useEffect hook - 600+ lines

**Problems:**
❌ Cannot test individual components in isolation  
❌ Single change requires testing entire file  
❌ High cognitive load - must understand everything  
❌ Merge conflicts inevitable with multiple developers  
❌ Code duplication across layout types  
❌ Difficult to add new styles without breaking existing ones  

### 2. Backend Structure (ASP.NET Core)

**Single Controller:** `DesignsController.cs` (600 lines)

**Responsibilities:**
1. HTTP endpoint handling
2. Style prompt parsing
3. Database queries
4. Room layout generation (5 different configurations)
5. Dimension calculations
6. OBJ export

**Key Method:** `GenerateRoomLayout(width, depth, roomCount, stories, shape)` - 400+ lines

**Problems:**
❌ Monolithic room generation logic  
❌ Room layout tied to room count, not architectural style  
❌ Cannot independently test room generation  
❌ Duplication of dimension calculations  

### 3. Data Models

**StyleTemplate:** Single class with all style properties
- Works reasonably well
- Could benefit from style-specific subclasses for complex styles

**HouseParameters:** God object with 20+ properties
- Needs decomposition into logical groups

---

## Refactored Architecture Design

### Philosophy: Composition Over Inheritance + Strategy Pattern

Instead of giant conditional blocks, use **pluggable strategies** that implement common interfaces.

---

## Phase 1: Frontend Refactoring

### New Directory Structure

```
ArchitecturalDreamMachineFrontend/
├── components/
│   ├── HouseViewer3D.js (REFACTORED - orchestrator only, ~150 lines)
│   ├── renderers/
│   │   ├── SceneManager.js (Three.js scene setup, camera, lighting)
│   │   ├── LayoutRenderer.js (Base class for layout strategies)
│   │   ├── layouts/
│   │   │   ├── CubeLayout.js
│   │   │   ├── TwoStoryLayout.js
│   │   │   ├── LShapeLayout.js
│   │   │   ├── SplitLevelLayout.js
│   │   │   └── AngledLayout.js
│   │   ├── RoofRenderer.js (Base class for roof strategies)
│   │   ├── roofs/
│   │   │   ├── GabledRoof.js
│   │   │   ├── FlatRoof.js
│   │   │   ├── HipRoof.js (future)
│   │   │   └── MansardRoof.js (future)
│   │   ├── WindowRenderer.js (Base class)
│   │   ├── windows/
│   │   │   ├── ModernWindows.js (large, minimal)
│   │   │   ├── VictorianWindows.js (ornate, small)
│   │   │   └── StandardWindows.js
│   │   ├── InteriorWallRenderer.js
│   │   ├── FoundationRenderer.js
│   │   └── MaterialManager.js (material creation/caching)
│   ├── styles/
│   │   ├── StyleStrategy.js (Base interface)
│   │   ├── VictorianStyle.js
│   │   ├── ModernStyle.js
│   │   ├── BrutalistStyle.js
│   │   └── StyleFactory.js (creates appropriate strategy)
│   └── utils/
│       ├── GeometryHelpers.js (shared geometry calculations)
│       ├── DisposalManager.js (Three.js cleanup)
│       └── Constants.js (magic numbers)
├── tests/
│   ├── roofGeometry.test.js (EXISTING)
│   ├── layouts/
│   │   ├── CubeLayout.test.js
│   │   └── LShapeLayout.test.js
│   ├── roofs/
│   │   ├── GabledRoof.test.js
│   │   └── FlatRoof.test.js
│   └── styles/
│       ├── VictorianStyle.test.js
│       └── ModernStyle.test.js
```

---

### 1.1 SceneManager.js

**Responsibility:** Three.js scene initialization, camera, lighting, ground plane

```javascript
// components/renderers/SceneManager.js
export class SceneManager {
  constructor(mountElement, width, height) {
    this.scene = new THREE.Scene();
    this.camera = new THREE.PerspectiveCamera(50, width / height, 0.1, 1000);
    this.renderer = new THREE.WebGLRenderer({ antialias: true });
    this.mountElement = mountElement;
    
    this._initScene();
    this._initLighting();
    this._initGround();
  }
  
  _initScene() {
    this.scene.background = new THREE.Color(0xf0f0f0);
    this.renderer.setSize(this.mountElement.clientWidth, 400);
    this.renderer.shadowMap.enabled = true;
    this.mountElement.appendChild(this.renderer.domElement);
  }
  
  _initLighting() {
    const ambient = new THREE.AmbientLight(0xffffff, 0.6);
    this.scene.add(ambient);
    
    const directional1 = new THREE.DirectionalLight(0xffffff, 0.8);
    directional1.position.set(10, 20, 10);
    directional1.castShadow = true;
    this.scene.add(directional1);
    
    const directional2 = new THREE.DirectionalLight(0xffffff, 0.4);
    directional2.position.set(-10, 10, -10);
    this.scene.add(directional2);
  }
  
  _initGround() {
    const groundGeometry = new THREE.PlaneGeometry(200, 200);
    const groundMaterial = new THREE.MeshStandardMaterial({ 
      color: 0x7cb342,
      roughness: 0.8
    });
    const ground = new THREE.Mesh(groundGeometry, groundMaterial);
    ground.rotation.x = -Math.PI / 2;
    ground.position.y = -0.1;
    ground.receiveShadow = true;
    this.scene.add(ground);
  }
  
  positionCamera(buildingSize, buildingHeight) {
    const distance = Math.max(buildingSize * 2.2, 60);
    this.camera.position.set(distance, buildingHeight * 0.8, distance);
    this.camera.lookAt(0, buildingHeight * 0.4, 0);
  }
  
  render() {
    this.renderer.render(this.scene, this.camera);
  }
  
  dispose() {
    this.renderer.dispose();
    if (this.mountElement.contains(this.renderer.domElement)) {
      this.mountElement.removeChild(this.renderer.domElement);
    }
  }
}
```

**Test:** `tests/SceneManager.test.js`
- Verify scene, camera, renderer initialized
- Check lighting setup (2 directional + 1 ambient)
- Confirm ground plane exists
- Test camera positioning for various building sizes

---

### 1.2 LayoutRenderer.js (Base Class)

**Responsibility:** Define interface for all layout strategies

```javascript
// components/renderers/LayoutRenderer.js
export class LayoutRenderer {
  constructor(houseGroup, params) {
    this.houseGroup = houseGroup;
    this.params = params;
  }
  
  /**
   * Calculate building dimensions specific to this layout
   * @returns {Object} { width, depth, mainHeight, sections: [{ width, height, depth, x, y, z }] }
   */
  calculateDimensions() {
    throw new Error('Must implement calculateDimensions()');
  }
  
  /**
   * Render the building structure
   * @param {MaterialManager} materialManager
   * @param {WindowRenderer} windowRenderer
   */
  render(materialManager, windowRenderer) {
    throw new Error('Must implement render()');
  }
  
  /**
   * Get roof attachment points for this layout
   * @returns {Array<{width, depth, x, y, z}>} Array of roof section specifications
   */
  getRoofSections() {
    throw new Error('Must implement getRoofSections()');
  }
}
```

---

### 1.3 CubeLayout.js (Concrete Implementation)

**Responsibility:** Single rectangular building, all stories same size

```javascript
// components/renderers/layouts/CubeLayout.js
import { LayoutRenderer } from '../LayoutRenderer.js';
import * as THREE from 'three';

export class CubeLayout extends LayoutRenderer {
  calculateDimensions() {
    const { footprintWidth, footprintDepth, ceilingHeight, stories } = this.params;
    
    return {
      width: footprintWidth,
      depth: footprintDepth,
      mainHeight: ceilingHeight * stories,
      sections: [{
        width: footprintWidth,
        height: ceilingHeight * stories,
        depth: footprintDepth,
        x: 0,
        y: (ceilingHeight * stories) / 2,
        z: 0
      }]
    };
  }
  
  render(materialManager, windowRenderer) {
    const dims = this.calculateDimensions();
    const section = dims.sections[0];
    
    // Create main building box
    const geometry = new THREE.BoxGeometry(section.width, section.height, section.depth);
    const material = materialManager.getExteriorMaterial();
    const building = new THREE.Mesh(geometry, material);
    building.position.set(section.x, section.y, section.z);
    building.castShadow = true;
    building.receiveShadow = true;
    this.houseGroup.add(building);
    
    // Add windows for each floor
    for (let floor = 1; floor <= this.params.stories; floor++) {
      const floorY = (floor - 0.5) * this.params.ceilingHeight;
      windowRenderer.addWindowsToWalls(
        this.houseGroup,
        section.width,
        this.params.ceilingHeight,
        section.depth,
        0,
        floorY,
        0,
        floor
      );
    }
    
    // Add floor platforms for upper stories
    if (this.params.stories > 1) {
      this._addFloorPlatforms(section);
    }
  }
  
  _addFloorPlatforms(section) {
    for (let floor = 2; floor <= this.params.stories; floor++) {
      const floorGeometry = new THREE.BoxGeometry(section.width, 0.5, section.depth);
      const floorMaterial = new THREE.MeshStandardMaterial({
        color: 0x8b7355,
        roughness: 0.8
      });
      const floorPlatform = new THREE.Mesh(floorGeometry, floorMaterial);
      floorPlatform.position.set(
        0,
        (floor - 1) * this.params.ceilingHeight - 0.25,
        0
      );
      floorPlatform.castShadow = true;
      this.houseGroup.add(floorPlatform);
    }
  }
  
  getRoofSections() {
    const dims = this.calculateDimensions();
    return [{
      width: dims.width,
      depth: dims.depth,
      x: 0,
      y: dims.mainHeight,
      z: 0
    }];
  }
}
```

**Test:** `tests/layouts/CubeLayout.test.js`
- Verify dimensions match footprint × stories
- Check single building section created
- Confirm floor platforms for multi-story
- Validate roof section returned

---

### 1.4 LShapeLayout.js (Concrete Implementation)

**Responsibility:** L-shaped building with two perpendicular wings

```javascript
// components/renderers/layouts/LShapeLayout.js
import { LayoutRenderer } from '../LayoutRenderer.js';
import * as THREE from 'three';

export class LShapeLayout extends LayoutRenderer {
  calculateDimensions() {
    const { footprintWidth: baseSize, footprintDepth: baseDepth, ceilingHeight, stories } = this.params;
    
    // Main wing: full width × 60% depth
    const mainWingWidth = baseSize;
    const mainWingDepth = baseDepth * 0.6;
    
    // Side wing: 50% width × 60% depth
    const sideWingWidth = baseSize * 0.5;
    const sideWingDepth = baseDepth * 0.6;
    
    const mainHeight = ceilingHeight * stories;
    
    return {
      width: baseSize,
      depth: baseDepth,
      mainHeight: mainHeight,
      sections: [
        {
          // Main wing
          width: mainWingWidth,
          height: mainHeight,
          depth: mainWingDepth,
          x: 0,
          y: mainHeight / 2,
          z: -baseDepth * 0.2 // Offset back
        },
        {
          // Side wing
          width: sideWingWidth,
          height: mainHeight,
          depth: sideWingDepth,
          x: baseSize * 0.25, // Offset right
          y: mainHeight / 2,
          z: baseDepth * 0.2 // Offset front
        }
      ]
    };
  }
  
  render(materialManager, windowRenderer) {
    const dims = this.calculateDimensions();
    
    dims.sections.forEach((section, index) => {
      const geometry = new THREE.BoxGeometry(section.width, section.height, section.depth);
      const material = materialManager.getExteriorMaterial();
      const building = new THREE.Mesh(geometry, material);
      building.position.set(section.x, section.y, section.z);
      building.castShadow = true;
      building.receiveShadow = true;
      this.houseGroup.add(building);
      
      // Add windows
      for (let floor = 1; floor <= this.params.stories; floor++) {
        const floorY = (floor - 0.5) * this.params.ceilingHeight;
        windowRenderer.addWindowsToWalls(
          this.houseGroup,
          section.width,
          this.params.ceilingHeight,
          section.depth,
          section.x,
          floorY,
          section.z,
          floor
        );
      }
    });
  }
  
  getRoofSections() {
    const dims = this.calculateDimensions();
    return dims.sections.map(section => ({
      width: section.width,
      depth: section.depth,
      x: section.x,
      y: dims.mainHeight,
      z: section.z
    }));
  }
}
```

---

### 1.5 RoofRenderer.js (Base Class)

**Responsibility:** Define interface for all roof types

```javascript
// components/renderers/RoofRenderer.js
export class RoofRenderer {
  constructor(roofParams) {
    this.roofType = roofParams.roofType;
    this.roofPitch = roofParams.roofPitch || 6.0;
    this.hasEaves = roofParams.hasEaves !== false;
    this.overhang = this.hasEaves ? (roofParams.eavesOverhang || 1.5) : 0;
    this.hasParapet = roofParams.hasParapet || false;
  }
  
  /**
   * Create roof geometry for given section
   * @param {Object} section { width, depth }
   * @returns {Object} { geometry, height }
   */
  createRoofGeometry(section) {
    throw new Error('Must implement createRoofGeometry()');
  }
  
  /**
   * Get material for this roof type
   * @returns {THREE.Material}
   */
  getMaterial() {
    throw new Error('Must implement getMaterial()');
  }
  
  /**
   * Render complete roof with all sections
   * @param {THREE.Group} houseGroup
   * @param {Array<Object>} roofSections
   */
  render(houseGroup, roofSections) {
    const material = this.getMaterial();
    
    roofSections.forEach(section => {
      const { geometry, height } = this.createRoofGeometry(section);
      const roof = new THREE.Mesh(geometry, material);
      roof.position.set(section.x, section.y, section.z);
      roof.castShadow = true;
      houseGroup.add(roof);
    });
  }
}
```

---

### 1.6 GabledRoof.js (Concrete Implementation)

**Responsibility:** Traditional gabled roof with ridge beam

```javascript
// components/renderers/roofs/GabledRoof.js
import { RoofRenderer } from '../RoofRenderer.js';
import * as THREE from 'three';

export class GabledRoof extends RoofRenderer {
  createRoofGeometry(section) {
    const roofWidth = section.width + (this.overhang * 2);
    const roofDepth = section.depth + (this.overhang * 2);
    
    // Calculate pitch ratio (rise/run)
    const pitchRatio = this.roofPitch / 12;
    
    // Height based on half the width (distance from ridge to eave)
    const roofHeight = (roofWidth / 2) * pitchRatio;
    
    console.log(`Gabled roof: span=${roofWidth.toFixed(1)}, height=${roofHeight.toFixed(1)}, pitch=${this.roofPitch}:12`);
    
    // Create custom gabled roof geometry
    const geometry = new THREE.BufferGeometry();
    
    const hw = roofWidth / 2;
    const hd = roofDepth / 2;
    
    // Vertices: ridge beam (2 points) + base corners (4 points)
    const vertices = new Float32Array([
      // Ridge beam (top center, front to back)
      0, roofHeight, hd,     // 0: Ridge front
      0, roofHeight, -hd,    // 1: Ridge back
      
      // Base corners (eaves level)
      -hw, 0, hd,            // 2: Left front eave
      hw, 0, hd,             // 3: Right front eave
      hw, 0, -hd,            // 4: Right back eave
      -hw, 0, -hd,           // 5: Left back eave
    ]);
    
    // Triangular faces
    const indices = [
      // Left slope
      0, 2, 5,
      0, 5, 1,
      
      // Right slope
      0, 3, 4,
      0, 4, 1,
      
      // Front gable
      0, 3, 2,
      
      // Back gable
      1, 5, 4,
    ];
    
    geometry.setAttribute('position', new THREE.BufferAttribute(vertices, 3));
    geometry.setIndex(indices);
    geometry.computeVertexNormals();
    
    return { geometry, height: roofHeight };
  }
  
  getMaterial() {
    return new THREE.MeshStandardMaterial({
      color: 0x8b4513, // Brown roof
      roughness: 0.9,
      side: THREE.DoubleSide // Visible from all angles
    });
  }
}
```

**Test:** `tests/roofs/GabledRoof.test.js`
- Verify height calculation: (width/2) × pitch_ratio
- Check overhang applied to width and depth
- Confirm double-sided material
- Validate 6 vertices, 8 triangles

---

### 1.7 FlatRoof.js (Concrete Implementation)

**Responsibility:** Flat roof with optional parapet

```javascript
// components/renderers/roofs/FlatRoof.js
import { RoofRenderer } from '../RoofRenderer.js';
import * as THREE from 'three';

export class FlatRoof extends RoofRenderer {
  createRoofGeometry(section) {
    const roofWidth = section.width + (this.overhang * 2);
    const roofDepth = section.depth + (this.overhang * 2);
    
    // Flat roof is just a thin box
    const geometry = new THREE.BoxGeometry(roofWidth, 0.75, roofDepth);
    
    return { geometry, height: 0 };
  }
  
  getMaterial() {
    return new THREE.MeshStandardMaterial({
      color: 0x333333, // Dark gray
      roughness: 0.8
    });
  }
  
  render(houseGroup, roofSections) {
    // Call parent to add flat roof
    super.render(houseGroup, roofSections);
    
    // Add parapet if specified
    if (this.hasParapet) {
      this._addParapet(houseGroup, roofSections);
    }
  }
  
  _addParapet(houseGroup, roofSections) {
    const parapetHeight = 2.5;
    const parapetThickness = 0.5;
    
    roofSections.forEach(section => {
      const roofWidth = section.width + (this.overhang * 2);
      const roofDepth = section.depth + (this.overhang * 2);
      
      // Front and back parapet walls
      [roofDepth / 2, -roofDepth / 2].forEach(zPos => {
        const parapetGeom = new THREE.BoxGeometry(
          roofWidth + parapetThickness,
          parapetHeight,
          parapetThickness
        );
        const parapetMat = new THREE.MeshStandardMaterial({
          color: 0xe0e0e0,
          roughness: 0.95
        });
        const parapet = new THREE.Mesh(parapetGeom, parapetMat);
        parapet.position.set(
          section.x,
          section.y + parapetHeight / 2,
          section.z + zPos
        );
        parapet.castShadow = true;
        houseGroup.add(parapet);
      });
      
      // Left and right parapet walls
      [roofWidth / 2, -roofWidth / 2].forEach(xPos => {
        const parapetGeom = new THREE.BoxGeometry(
          parapetThickness,
          parapetHeight,
          roofDepth
        );
        const parapetMat = new THREE.MeshStandardMaterial({
          color: 0xe0e0e0,
          roughness: 0.95
        });
        const parapet = new THREE.Mesh(parapetGeom, parapetMat);
        parapet.position.set(
          section.x + xPos,
          section.y + parapetHeight / 2,
          section.z
        );
        parapet.castShadow = true;
        houseGroup.add(parapet);
      });
    });
  }
}
```

---

### 1.8 StyleStrategy.js (Base Class)

**Responsibility:** Coordinate all renderers for a specific architectural style

```javascript
// components/styles/StyleStrategy.js
export class StyleStrategy {
  constructor(houseParams) {
    this.params = houseParams;
  }
  
  /**
   * Get appropriate layout renderer for this style
   * @returns {LayoutRenderer}
   */
  getLayoutRenderer(houseGroup) {
    throw new Error('Must implement getLayoutRenderer()');
  }
  
  /**
   * Get appropriate roof renderer for this style
   * @returns {RoofRenderer}
   */
  getRoofRenderer() {
    throw new Error('Must implement getRoofRenderer()');
  }
  
  /**
   * Get appropriate window renderer for this style
   * @returns {WindowRenderer}
   */
  getWindowRenderer() {
    throw new Error('Must implement getWindowRenderer()');
  }
  
  /**
   * Get material parameters for this style
   * @returns {Object}
   */
  getMaterialParams() {
    return {
      exteriorMaterial: this.params.exteriorMaterial || 'stucco',
      color: this.params.material?.color || 'white',
      roughness: 0.7
    };
  }
}
```

---

### 1.9 VictorianStyle.js (Concrete Implementation)

**Responsibility:** Orchestrate Victorian-specific rendering choices

```javascript
// components/styles/VictorianStyle.js
import { StyleStrategy } from './StyleStrategy.js';
import { LShapeLayout } from '../renderers/layouts/LShapeLayout.js';
import { TwoStoryLayout } from '../renderers/layouts/TwoStoryLayout.js';
import { GabledRoof } from '../renderers/roofs/GabledRoof.js';
import { VictorianWindows } from '../renderers/windows/VictorianWindows.js';

export class VictorianStyle extends StyleStrategy {
  getLayoutRenderer(houseGroup) {
    // Victorian typically uses L-shape if multi-room, two-story if 2+ stories
    if (this.params.buildingShape === 'l-shape' || this.params.roomCount >= 6) {
      return new LShapeLayout(houseGroup, this.params);
    } else if (this.params.stories >= 2) {
      return new TwoStoryLayout(houseGroup, this.params);
    } else {
      // Fall back to cube for small single-story
      return new CubeLayout(houseGroup, this.params);
    }
  }
  
  getRoofRenderer() {
    // Victorian always uses steep gabled roof (8:12 pitch)
    return new GabledRoof({
      roofType: 'gabled',
      roofPitch: this.params.roofPitch || 8.0,
      hasEaves: true,
      eavesOverhang: 2.0, // Victorian has generous overhang
      hasParapet: false
    });
  }
  
  getWindowRenderer() {
    // Victorian uses ornate, small windows
    return new VictorianWindows({
      windowStyle: 'ornate',
      windowToWallRatio: this.params.windowToWallRatio || 0.20
    });
  }
  
  getMaterialParams() {
    return {
      exteriorMaterial: 'wood siding',
      color: this.params.material?.color || 'cream',
      roughness: 0.8
    };
  }
}
```

---

### 1.10 BrutalistStyle.js (Concrete Implementation)

```javascript
// components/styles/BrutalistStyle.js
import { StyleStrategy } from './StyleStrategy.js';
import { CubeLayout } from '../renderers/layouts/CubeLayout.js';
import { FlatRoof } from '../renderers/roofs/FlatRoof.js';
import { ModernWindows } from '../renderers/windows/ModernWindows.js';

export class BrutalistStyle extends StyleStrategy {
  getLayoutRenderer(houseGroup) {
    // Brutalist typically uses simple cube or split-level
    if (this.params.buildingShape === 'split-level') {
      return new SplitLevelLayout(houseGroup, this.params);
    } else {
      return new CubeLayout(houseGroup, this.params);
    }
  }
  
  getRoofRenderer() {
    // Brutalist always uses flat roof with parapet
    return new FlatRoof({
      roofType: 'flat',
      roofPitch: 0,
      hasEaves: false,
      eavesOverhang: 0,
      hasParapet: true
    });
  }
  
  getWindowRenderer() {
    // Brutalist uses minimal small windows
    return new ModernWindows({
      windowStyle: 'small',
      windowToWallRatio: this.params.windowToWallRatio || 0.10
    });
  }
  
  getMaterialParams() {
    return {
      exteriorMaterial: 'concrete',
      color: this.params.material?.color || 'gray',
      roughness: 0.95
    };
  }
}
```

---

### 1.11 StyleFactory.js

**Responsibility:** Create appropriate style strategy based on params

```javascript
// components/styles/StyleFactory.js
import { VictorianStyle } from './VictorianStyle.js';
import { ModernStyle } from './ModernStyle.js';
import { BrutalistStyle } from './BrutalistStyle.js';

export class StyleFactory {
  static createStyle(houseParams) {
    // Determine style from styleName or other params
    const styleName = (houseParams.styleName || '').toLowerCase();
    
    if (styleName.includes('victorian')) {
      return new VictorianStyle(houseParams);
    } else if (styleName.includes('brutalist')) {
      return new BrutalistStyle(houseParams);
    } else if (styleName.includes('modern')) {
      return new ModernStyle(houseParams);
    } else {
      // Default to modern
      console.warn('Unknown style, defaulting to Modern');
      return new ModernStyle(houseParams);
    }
  }
}
```

---

### 1.12 Refactored HouseViewer3D.js

**Responsibility:** Orchestration only - delegates to strategies

```javascript
// components/HouseViewer3D.js (REFACTORED - 150 lines)
import React, { useEffect, useRef } from 'react';
import { SceneManager } from './renderers/SceneManager.js';
import { StyleFactory } from './styles/StyleFactory.js';
import { MaterialManager } from './renderers/MaterialManager.js';
import { InteriorWallRenderer } from './renderers/InteriorWallRenderer.js';
import { FoundationRenderer } from './renderers/FoundationRenderer.js';
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
      
      // Create style strategy (determines all rendering choices)
      const style = StyleFactory.createStyle(houseParams);
      
      // Create managers
      const materialManager = new MaterialManager(style.getMaterialParams());
      
      // Get renderers from style
      const layoutRenderer = style.getLayoutRenderer(houseGroup);
      const roofRenderer = style.getRoofRenderer();
      const windowRenderer = style.getWindowRenderer();
      
      // Render foundation
      const foundationRenderer = new FoundationRenderer(houseParams);
      foundationRenderer.render(houseGroup, layoutRenderer.calculateDimensions());
      
      // Render building structure
      layoutRenderer.render(materialManager, windowRenderer);
      
      // Render roof
      const roofSections = layoutRenderer.getRoofSections();
      roofRenderer.render(houseGroup, roofSections);
      
      // Render interior walls
      if (houseParams.rooms && houseParams.rooms.length > 0) {
        const interiorWallRenderer = new InteriorWallRenderer(
          layoutRenderer.calculateDimensions()
        );
        interiorWallRenderer.render(houseGroup, houseParams.rooms);
      }
      
      // Position camera
      const dims = layoutRenderer.calculateDimensions();
      const roofSectionHeights = roofSections.map(s => 
        roofRenderer.createRoofGeometry(s).height
      );
      const maxRoofHeight = Math.max(...roofSectionHeights);
      sceneManager.positionCamera(
        Math.max(dims.width, dims.depth),
        dims.mainHeight + maxRoofHeight
      );
      
      // Animation loop
      const animate = () => {
        frameId = requestAnimationFrame(animate);
        houseGroup.rotation.y += 0.003; // Slow rotation
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
        
        // Dispose all Three.js resources
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

**Benefits:**
- ✅ Only 150 lines (was 942)
- ✅ No conditional logic - delegates to strategies
- ✅ Easy to test - mock strategies
- ✅ Clear separation of concerns

---

## Phase 2: Backend Refactoring

### New Directory Structure

```
ArchitecturalDreamMachineBackend/
├── Controllers/
│   └── DesignsController.cs (REFACTORED - ~150 lines)
├── Services/
│   ├── IDesignService.cs (interface)
│   ├── DesignService.cs (orchestration)
│   ├── StyleService.cs (style template queries)
│   └── RoomLayoutService.cs (room generation)
├── RoomLayouts/
│   ├── IRoomLayoutStrategy.cs
│   ├── SmallHouseLayout.cs (3 rooms)
│   ├── MediumHouseLayout.cs (5 rooms)
│   ├── LargeHouseLayout.cs (6+ rooms)
│   └── RoomLayoutFactory.cs
├── Geometry/
│   ├── DimensionCalculator.cs (extract calculations)
│   └── (existing classes)
├── Export/
│   └── ObjExporter.cs (existing)
```

---

### 2.1 IDesignService.cs

```csharp
// Services/IDesignService.cs
public interface IDesignService
{
    Task<HouseParameters> GenerateDesign(GenerateRequest request);
    Task<HouseParameters> GenerateWithOverride(GenerateWithOverrideRequest request);
}
```

---

### 2.2 StyleService.cs

**Responsibility:** Query StyleTemplate, handle defaults

```csharp
// Services/StyleService.cs
public class StyleService
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<StyleService> _logger;
    
    public StyleService(AppDbContext context, IMemoryCache cache, ILogger<StyleService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }
    
    public async Task<StyleTemplate?> GetStyleByKeyword(string keyword)
    {
        var cacheKey = $"style_{keyword.ToLower()}";
        
        if (_cache.TryGetValue(cacheKey, out StyleTemplate? cached))
        {
            _logger.LogDebug("Cache hit for keyword: {Keyword}", keyword);
            return cached;
        }
        
        var template = await _context.StyleTemplates
            .FirstOrDefaultAsync(st => st.Name.ToLower().Contains(keyword));
        
        if (template != null)
        {
            _cache.Set(cacheKey, template, TimeSpan.FromHours(1));
            _logger.LogDebug("Cached style template for keyword: {Keyword}", keyword);
        }
        
        return template;
    }
    
    public async Task<StyleTemplate> GetStyleByKeywords(IEnumerable<string> keywords)
    {
        foreach (var keyword in keywords)
        {
            var style = await GetStyleByKeyword(keyword);
            if (style != null) return style;
        }
        
        // Fallback to Modern
        _logger.LogWarning("No style matched keywords: {Keywords}, using Modern", string.Join(", ", keywords));
        return await GetStyleByKeyword("modern") 
            ?? throw new InvalidOperationException("No default style available");
    }
}
```

---

### 2.3 IRoomLayoutStrategy.cs

```csharp
// RoomLayouts/IRoomLayoutStrategy.cs
public interface IRoomLayoutStrategy
{
    List<Room> GenerateLayout(
        double width,
        double depth,
        int stories,
        string buildingShape);
}
```

---

### 2.4 SmallHouseLayout.cs

**Responsibility:** 3-room layout (living, bedroom, bath)

```csharp
// RoomLayouts/SmallHouseLayout.cs
public class SmallHouseLayout : IRoomLayoutStrategy
{
    public List<Room> GenerateLayout(double width, double depth, int stories, string buildingShape)
    {
        return new List<Room>
        {
            new Room
            {
                Name = "Living Room",
                Floor = 1,
                X = 0,
                Z = 0,
                Width = width,
                Depth = depth * 0.6,
                WindowCount = CalculateWindowCount(width, depth * 0.6, 0.15),
                HasDoor = true
            },
            new Room
            {
                Name = "Bedroom",
                Floor = 1,
                X = 0,
                Z = depth * 0.6,
                Width = width * 0.6,
                Depth = depth * 0.4,
                WindowCount = 1, // Egress required
                HasDoor = true
            },
            new Room
            {
                Name = "Bathroom",
                Floor = 1,
                X = width * 0.6,
                Z = depth * 0.6,
                Width = width * 0.4,
                Depth = depth * 0.4,
                WindowCount = 0,
                HasDoor = true
            }
        };
    }
    
    private int CalculateWindowCount(double width, double depth, double windowToWallRatio)
    {
        double perimeter = 2 * (width + depth);
        double wallArea = perimeter * 9.0; // 9ft ceiling
        double targetWindowArea = wallArea * windowToWallRatio;
        int windowCount = (int)Math.Ceiling(targetWindowArea / 12.0); // 12 sqft windows
        return Math.Max(1, Math.Min(5, windowCount));
    }
}
```

---

### 2.5 RoomLayoutService.cs

**Responsibility:** Delegate to appropriate layout strategy

```csharp
// Services/RoomLayoutService.cs
public class RoomLayoutService
{
    private readonly ILogger<RoomLayoutService> _logger;
    
    public RoomLayoutService(ILogger<RoomLayoutService> logger)
    {
        _logger = logger;
    }
    
    public List<Room> GenerateRoomLayout(
        double width,
        double depth,
        int roomCount,
        int stories,
        string shape)
    {
        _logger.LogInformation("Generating room layout: {RoomCount} rooms, {Stories} stories", roomCount, stories);
        
        IRoomLayoutStrategy strategy = roomCount switch
        {
            <= 3 => new SmallHouseLayout(),
            <= 5 => new MediumHouseLayout(),
            _ => new LargeHouseLayout()
        };
        
        return strategy.GenerateLayout(width, depth, stories, shape);
    }
}
```

---

### 2.6 DesignService.cs

**Responsibility:** Orchestrate design generation

```csharp
// Services/DesignService.cs
public class DesignService : IDesignService
{
    private readonly AppDbContext _context;
    private readonly StyleService _styleService;
    private readonly RoomLayoutService _roomLayoutService;
    private readonly ILogger<DesignService> _logger;
    
    public DesignService(
        AppDbContext context,
        StyleService styleService,
        RoomLayoutService roomLayoutService,
        ILogger<DesignService> logger)
    {
        _context = context;
        _styleService = styleService;
        _roomLayoutService = roomLayoutService;
        _logger = logger;
    }
    
    public async Task<HouseParameters> GenerateDesign(GenerateRequest request)
    {
        // Parse keywords
        var keywords = PromptParser.Parse(request.StylePrompt);
        _logger.LogInformation("Parsed keywords: {Keywords}", string.Join(", ", keywords));
        
        // Get style template
        var styleTemplate = await _styleService.GetStyleByKeywords(keywords);
        
        // Calculate dimensions
        var dimensions = DimensionCalculator.Calculate(request.LotSize, styleTemplate.TypicalStories);
        
        // Generate room layout
        var rooms = _roomLayoutService.GenerateRoomLayout(
            dimensions.FootprintWidth,
            dimensions.FootprintDepth,
            styleTemplate.RoomCount,
            styleTemplate.TypicalStories,
            styleTemplate.BuildingShape
        );
        
        // Create HouseParameters
        var houseParameters = new HouseParameters
        {
            LotSize = request.LotSize,
            RoofType = styleTemplate.RoofType,
            WindowStyle = styleTemplate.WindowStyle,
            RoomCount = styleTemplate.RoomCount,
            Material = new Material
            {
                Color = styleTemplate.Color,
                Texture = styleTemplate.Texture
            },
            CeilingHeight = styleTemplate.TypicalCeilingHeight,
            Stories = styleTemplate.TypicalStories,
            BuildingShape = styleTemplate.BuildingShape,
            WindowToWallRatio = styleTemplate.WindowToWallRatio,
            FoundationType = styleTemplate.FoundationType,
            ExteriorMaterial = styleTemplate.ExteriorMaterial,
            FootprintWidth = dimensions.FootprintWidth,
            FootprintDepth = dimensions.FootprintDepth,
            RoofPitch = styleTemplate.RoofPitch,
            HasParapet = styleTemplate.HasParapet,
            HasEaves = styleTemplate.HasEaves,
            EavesOverhang = styleTemplate.EavesOverhang,
            Rooms = rooms
        };
        
        // Save to database
        var design = new Design
        {
            LotSize = request.LotSize,
            StyleKeywords = string.Join(", ", keywords)
        };
        _context.Designs.Add(design);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Created design ID {DesignId} using style {StyleName}", 
            design.Id, styleTemplate.Name);
        
        return houseParameters;
    }
    
    public async Task<HouseParameters> GenerateWithOverride(GenerateWithOverrideRequest request)
    {
        // Similar to GenerateDesign but uses request.Stories instead of styleTemplate.TypicalStories
        // ... implementation
    }
}
```

---

### 2.7 Refactored DesignsController.cs

```csharp
// Controllers/DesignsController.cs (REFACTORED - ~150 lines)
[ApiController]
[Route("api/[controller]")]
public class DesignsController : ControllerBase
{
    private readonly IDesignService _designService;
    private readonly AppDbContext _context;
    private readonly ILogger<DesignsController> _logger;
    
    public DesignsController(
        IDesignService designService,
        AppDbContext context,
        ILogger<DesignsController> logger)
    {
        _designService = designService;
        _context = context;
        _logger = logger;
    }
    
    [HttpPost("generate")]
    public async Task<ActionResult<HouseParameters>> Generate([FromBody] GenerateRequest request)
    {
        if (request.LotSize <= 0)
            return BadRequest(new { error = "Lot size must be greater than 0" });
        
        if (string.IsNullOrWhiteSpace(request.StylePrompt))
            return BadRequest(new { error = "Style prompt is required" });
        
        try
        {
            var houseParameters = await _designService.GenerateDesign(request);
            
            return Ok(new
            {
                houseParameters,
                mesh = houseParameters.GenerateMesh(),
                designId = houseParameters.DesignId, // Added to HouseParameters
                styleName = houseParameters.StyleName // Added to HouseParameters
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating design");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Design>>> GetAll()
    {
        // ... existing implementation
    }
    
    [HttpGet("{id}/export")]
    public async Task<IActionResult> ExportToObj(int id)
    {
        // ... existing implementation (extract to ExportService)
    }
}
```

**Benefits:**
- ✅ Only 150 lines (was 600)
- ✅ No business logic in controller
- ✅ Easy to test - mock IDesignService
- ✅ Services reusable elsewhere

---

## Implementation Sequence (November 25, 2025)

**PRIORITY: Backend First** - Backend does heavy lifting, frontend compiles data

### Step 1: Backend Geometry Services (1.5 hours) ⭐ START HERE
**Goal:** Backend calculates all geometry, not frontend
1. Create `Services/GeometryService.cs` - Calculate vertices, faces, dimensions
2. Create `Geometry/VertexCalculator.cs` - Roof vertices, wall vertices
3. Create `Geometry/FaceGenerator.cs` - Generate triangular faces
4. Create `Models/GeometryData.cs` - DTOs for vertices/faces
5. **Test:** Calculate gabled roof vertices, return to frontend

### Step 2: Backend Layout Services (1.5 hours) ⭐
**Goal:** Backend determines layout strategy, calculates sections
1. Create `Services/LayoutService.cs` - Determine layout based on style
2. Create `LayoutStrategies/ILayoutStrategy.cs`
3. Create `LayoutStrategies/CubeLayoutStrategy.cs`
4. Create `LayoutStrategies/LShapeLayoutStrategy.cs`
5. Create `Models/LayoutData.cs` - Sections with positions/dimensions
6. **Test:** Layout service returns correct sections for Victorian L-shape

### Step 3: Backend Roof Services (1 hour) ⭐
**Goal:** Backend calculates roof geometry (vertices, faces, height)
1. Create `Services/RoofService.cs` - Determine roof type, calculate geometry
2. Create `RoofStrategies/IRoofStrategy.cs`
3. Create `RoofStrategies/GabledRoofStrategy.cs` - Port Phase 1.1 fix
4. Create `RoofStrategies/FlatRoofStrategy.cs`
5. Create `Models/RoofData.cs` - Vertices, faces, height, overhang
6. **Test:** Gabled roof returns 6 vertices, 8 faces, correct height

### Step 4: Backend Style Services (30 minutes)
1. Create `Services/StyleService.cs` with caching
2. Create `Services/DesignOrchestrationService.cs` - Coordinates all services
3. Update `DesignsController.cs` - Return complete geometry data
4. **Test:** API returns geometry ready for Three.js

### Step 5: Backend Room Layout Services (30 minutes)
1. Create `RoomLayouts/IRoomLayoutStrategy.cs`
2. Create `RoomLayouts/SmallHouseLayout.cs`
3. Create `RoomLayouts/MediumHouseLayout.cs`
4. Create `Services/RoomLayoutService.cs`
5. **Test:** Room service returns correct layouts

### Step 6: Frontend SceneManager (45 minutes)
**Goal:** Thin client - just render geometry from backend
1. Create `renderers/SceneManager.js` - Three.js initialization
2. Create `renderers/GeometryRenderer.js` - Convert backend data to Three.js
3. Create `utils/DisposalManager.js` - Cleanup
4. **Test:** Render backend geometry data

### Step 7: Refactor HouseViewer3D (45 minutes)
**Goal:** Fetch geometry from backend, render with Three.js
1. Remove all geometry calculations
2. Fetch complete geometry from `/api/designs/generate`
3. Render using `GeometryRenderer`
4. **Test:** Victorian, Modern, Brutalist all render from backend data

### Step 8: Integration Testing (1 hour)
1. Test API returns correct geometry for all styles
2. Test frontend renders backend geometry correctly
3. Compare visual output before/after
4. Test memory cleanup
5. Performance benchmarks

### Step 9: Documentation (30 minutes)
1. Update API documentation with new geometry endpoints
2. Document backend geometry calculation flow
3. Document how to add new styles/roofs in backend
4. Create sequence diagram: API → Geometry → Frontend

---

## Migration Strategy

### Option A: Big Bang (Not Recommended)
- Replace everything at once
- High risk of breaking changes
- Difficult to debug if issues arise

### Option B: Incremental Migration (Recommended)
1. **Week 1:** Create new structure alongside old code
2. **Week 2:** Migrate one style (Victorian) to new system
3. **Week 3:** Add feature flag to toggle between old/new
4. **Week 4:** Migrate remaining styles
5. **Week 5:** Remove old code once validated

### Feature Flag Example:
```javascript
const USE_MODULAR_ARCHITECTURE = true;

if (USE_MODULAR_ARCHITECTURE) {
  // Use new style strategy system
  const style = StyleFactory.createStyle(houseParams);
  // ...
} else {
  // Use legacy monolithic code
  // ... existing implementation
}
```

---

## Testing Strategy

### Unit Tests (Per Module)
- Each layout renderer tested independently
- Each roof renderer tested independently
- Each style strategy tested independently
- Mock dependencies

### Integration Tests
- Style factory creates correct strategies
- Strategies coordinate correctly
- Backend services integrate with controllers

### End-to-End Tests
- Generate design via API
- Verify 3D rendering
- Compare before/after refactor for visual parity

### Performance Tests
- Memory usage stable
- Render time < 100ms per regeneration
- Cache hit rate > 90% for style templates

---

## Success Criteria

### Functional Requirements
- ✅ All existing designs render identically before/after
- ✅ No regressions in roof, windows, walls, etc.
- ✅ Memory properly disposed
- ✅ CORS security maintained

### Non-Functional Requirements
- ✅ Each file < 200 lines (except complex geometry)
- ✅ No conditional style logic in HouseViewer3D.js
- ✅ 80%+ test coverage
- ✅ Add new style in < 30 minutes without touching existing code

### Developer Experience
- ✅ Clear documentation
- ✅ Easy to debug (small, focused modules)
- ✅ Fast test execution (< 30 seconds total)

---

## Benefits Summary

### Before Refactoring
- 942-line monolithic component
- Difficult to test
- High coupling
- Adding new style = 50+ line changes across file
- Merge conflicts frequent
- Cognitive overload

### After Refactoring
- ~10 files, each < 200 lines
- Each module independently testable
- Low coupling, high cohesion
- Adding new style = create one new class, zero changes to existing code
- Parallel development possible
- Easy to understand individual pieces

---

## Risk Mitigation

### Risk: Breaking Existing Functionality
**Mitigation:**
- Comprehensive test suite before refactor
- Feature flag for gradual rollout
- Visual regression testing

### Risk: Increased Complexity
**Mitigation:**
- Clear documentation
- Architecture diagram
- Developer onboarding guide

### Risk: Performance Degradation
**Mitigation:**
- Performance benchmarks before/after
- Profiling during development
- Caching strategies

### Risk: Over-Engineering
**Mitigation:**
- Keep base classes simple
- Only extract when clear benefit
- YAGNI principle (You Aren't Gonna Need It)

---

## Future Extensions (Post-Refactor)

With modular architecture, easily add:

### New Roof Types
- Hip roof (4-sided with no gables)
- Gambrel roof (barn-style)
- Mansard roof (French style)
- Butterfly roof (modern inverted)

### New Architectural Styles
- Colonial (symmetric, dormers)
- Craftsman (exposed rafters, wide porches)
- Mediterranean (tile roof, stucco, arches)
- Tudor (half-timbering, steep roofs)
- Art Deco (geometric, streamlined)

### New Layout Types
- Courtyard (U-shape with central open space)
- T-shape
- Circular/radial

### New Features
- Balconies
- Porches
- Chimneys
- Dormers
- Bay windows
- Turrets (Victorian)
- Pillars/columns (Classical)

### Advanced Rendering
- Material swapping
- Color palettes
- Texture mapping
- LOD (Level of Detail) for performance

---

## Maintenance Guidelines

### Adding a New Architectural Style

1. **Create style strategy class**
   ```javascript
   // components/styles/ColonialStyle.js
   export class ColonialStyle extends StyleStrategy {
     // Implement abstract methods
   }
   ```

2. **Register in StyleFactory**
   ```javascript
   // components/styles/StyleFactory.js
   if (styleName.includes('colonial')) {
     return new ColonialStyle(houseParams);
   }
   ```

3. **Add tests**
   ```javascript
   // tests/styles/ColonialStyle.test.js
   describe('ColonialStyle', () => {
     // Test layout selection
     // Test roof selection
     // Test window selection
   });
   ```

4. **Update documentation**
   - Add to supported styles list
   - Include architectural characteristics
   - Add example rendering

**Time to add new style:** < 1 hour (vs 4+ hours with monolithic code)

---

## Conclusion

This refactoring transforms the Architectural Dream Machine from a monolithic, hard-to-maintain codebase into a modular, extensible architecture that follows SOLID principles.

**Key Achievements:**
- 85% reduction in main component size (942 → 150 lines)
- Unlimited extensibility (add styles without touching existing code)
- Testability (each module independently tested)
- Maintainability (small, focused modules)
- Developer experience (clear separation of concerns)

**Investment:**
- 6-8 hours initial refactoring
- Pays dividends immediately
- Every new feature is faster and safer

**Next Steps:**
1. Review this plan with team
2. Set up feature branch: `refactor/modular-architecture`
3. Begin Step 1: Core infrastructure
4. Incremental development with continuous testing
5. Deploy with feature flag for gradual rollout

---

**This refactoring will make Phase 1.2-1.4 bug fixes cleaner and easier, as each component can be fixed in isolation without fear of breaking unrelated functionality.**
