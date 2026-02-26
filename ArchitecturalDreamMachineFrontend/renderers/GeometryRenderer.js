import * as THREE from 'three';

/**
 * GeometryRenderer - Converts backend geometry data to Three.js meshes
 * Backend provides complete geometry (vertices, indices) ready for rendering
 */
export class GeometryRenderer {
  /**
   * Create a Three.js mesh from backend geometry data
   * @param {Object} geometryData - Backend geometry data {vertices, indices, materialType, color}
   * @returns {THREE.Mesh} - Three.js mesh ready to render
   */
  static createMeshFromGeometry(geometryData) {
    if (!geometryData || !geometryData.vertices || !geometryData.indices) {
      console.error('Invalid geometry data:', geometryData);
      return null;
    }

    // Bounds checking to prevent memory exhaustion from malformed data
    const MAX_VERTICES = 300000; // 100,000 vertices * 3 components
    const MAX_INDICES = 300000;
    if (geometryData.vertices.length > MAX_VERTICES) {
      console.error('Geometry exceeds max vertex limit:', geometryData.vertices.length);
      return null;
    }
    if (geometryData.indices.length > MAX_INDICES) {
      console.error('Geometry exceeds max index limit:', geometryData.indices.length);
      return null;
    }
    if (geometryData.vertices.length % 3 !== 0) {
      console.error('Vertex array length not divisible by 3:', geometryData.vertices.length);
      return null;
    }

    const vertexCount = geometryData.vertices.length / 3;
    for (let i = 0; i < geometryData.indices.length; i++) {
      if (geometryData.indices[i] < 0 || geometryData.indices[i] >= vertexCount) {
        console.error('Index out of bounds:', geometryData.indices[i], 'max:', vertexCount - 1);
        return null;
      }
    }

    const geometry = new THREE.BufferGeometry();

    // Set vertices (backend provides flat array: [x,y,z, x,y,z, ...])
    geometry.setAttribute(
      'position',
      new THREE.BufferAttribute(new Float32Array(geometryData.vertices), 3)
    );

    // Set face indices (backend provides triangulated faces)
    geometry.setIndex(geometryData.indices);
    
    // Compute normals for lighting
    geometry.computeVertexNormals();
    
    // Create material
    const material = this.createMaterial(
      geometryData.materialType || 'standard',
      geometryData.color || '#ffffff'
    );
    
    const mesh = new THREE.Mesh(geometry, material);

    // Apply position if provided by backend
    if (geometryData.position) {
      mesh.position.set(
        geometryData.position.x || 0,
        geometryData.position.y || 0,
        geometryData.position.z || 0
      );
    }

    // Apply rotation if provided by backend (e.g. windows on left/right walls use rotation.y = PI/2)
    if (geometryData.rotation) {
      mesh.rotation.set(
        geometryData.rotation.x || 0,
        geometryData.rotation.y || 0,
        geometryData.rotation.z || 0
      );
    }

    return mesh;
  }

  /**
   * Create a Three.js mesh for one exterior wall face panel with punched-out
   * openings (windows and doors).  Uses THREE.ShapeGeometry with holes so the
   * openings are true geometric cut-outs, not quads placed on top.
   *
   * THREE.ShapeGeometry lives in the XY plane facing +Z.  RotationY aligns
   * each face to its correct world orientation:
   *   0      → Front  (faces +Z)
   *   π      → Back   (faces −Z)
   *  −π/2    → Right  (faces +X)
   *   π/2    → Left   (faces −X)
   *
   * Position (x, y, z): x/z = world face centre; y = section base (floor level).
   * Shape Y axis goes 0 → height (base to top of face).
   *
   * @param {Object} faceData - WallFaceData from backend
   * @returns {THREE.Mesh}
   */
  static createWallFacePanel(faceData) {
    if (!faceData || !faceData.width || !faceData.height) {
      console.warn('Invalid wall face data:', faceData);
      return null;
    }

    const hw = faceData.width / 2;

    // Outer wall shape (centered horizontally, 0..height vertically)
    const shape = new THREE.Shape();
    shape.moveTo(-hw, 0);
    shape.lineTo( hw, 0);
    shape.lineTo( hw, faceData.height);
    shape.lineTo(-hw, faceData.height);
    shape.closePath();

    // Punch a rectangular hole for each opening
    (faceData.openings || []).forEach(op => {
      if (!op.width || !op.height) return;
      const hole = new THREE.Path();
      const ox   = op.offsetX;
      const oy   = op.offsetY; // vertical centre from base
      const hw2  = op.width  / 2;
      const hh2  = op.height / 2;
      hole.moveTo(ox - hw2, oy - hh2);
      hole.lineTo(ox + hw2, oy - hh2);
      hole.lineTo(ox + hw2, oy + hh2);
      hole.lineTo(ox - hw2, oy + hh2);
      hole.closePath();
      shape.holes.push(hole);
    });

    const geometry = new THREE.ShapeGeometry(shape);
    const material = this.createMaterial(
      faceData.materialType || 'stucco',
      faceData.color         || '#ffffff'
    );
    material.side = THREE.DoubleSide; // solid wall areas visible from all angles; holes are geometry cut-outs

    const mesh = new THREE.Mesh(geometry, material);
    // y = section base; ShapeGeometry Y goes 0→height from that base
    mesh.position.set(faceData.x, faceData.y, faceData.z);
    mesh.rotation.y = faceData.rotationY;
    mesh.castShadow    = true;
    mesh.receiveShadow = true;
    return mesh;
  }

  /**
   * Create Three.js material based on type and color
   * @param {string} materialType - Material type (concrete, brick, glass, etc.)
   * @param {string} color - Hex color string
   * @returns {THREE.Material} - Three.js material
   */
  static createMaterial(materialType, color) {
    const materialConfig = {
      color: new THREE.Color(color),
      side: THREE.DoubleSide
    };

    switch (materialType.toLowerCase()) {
      case 'concrete':
        return new THREE.MeshStandardMaterial({
          ...materialConfig,
          roughness: 0.95,
          metalness: 0.0
        });
      
      case 'brick':
        return new THREE.MeshStandardMaterial({
          ...materialConfig,
          roughness: 0.85,
          metalness: 0.0
        });
      
      case 'glass':
        return new THREE.MeshPhysicalMaterial({
          ...materialConfig,
          roughness: 0.1,
          metalness: 0.0,
          transmission: 0.9,
          transparent: true,
          opacity: 0.3
        });
      
      case 'metal':
        return new THREE.MeshStandardMaterial({
          ...materialConfig,
          roughness: 0.3,
          metalness: 0.9
        });
      
      case 'wood':
      case 'wood siding':
        return new THREE.MeshStandardMaterial({
          ...materialConfig,
          roughness: 0.7,
          metalness: 0.0
        });
      
      case 'stucco':
        return new THREE.MeshStandardMaterial({
          ...materialConfig,
          roughness: 0.9,
          metalness: 0.0
        });
      
      case 'roof':
        return new THREE.MeshStandardMaterial({
          ...materialConfig,
          roughness: 0.8,
          metalness: 0.0
        });
      
      default:
        return new THREE.MeshStandardMaterial({
          ...materialConfig,
          roughness: 0.7,
          metalness: 0.0
        });
    }
  }

  /**
   * Render complete building from backend geometry
   * @param {THREE.Group} houseGroup - Three.js group to add meshes to
   * @param {Object} buildingGeometry - Complete building geometry from backend
   */
  static renderBuilding(houseGroup, buildingGeometry) {
    if (!buildingGeometry) {
      console.error('No building geometry provided');
      return;
    }

    // Clear existing meshes
    while (houseGroup.children.length > 0) {
      const child = houseGroup.children[0];
      houseGroup.remove(child);
      if (child.geometry) child.geometry.dispose();
      if (child.material) child.material.dispose();
    }

    // Add building sections
    if (buildingGeometry.sections && Array.isArray(buildingGeometry.sections)) {
      buildingGeometry.sections.forEach((sectionData, index) => {
        const mesh = this.createMeshFromGeometry(sectionData);
        if (mesh) {
          // FrontSide: section box provides solid-wall fallback for any face where
          // the wall-face panel was suppressed (interior seam).  Face panels (0.02 ft
          // proud) overlay it with true window/door holes wherever they exist.
          mesh.material.side = THREE.FrontSide;
          mesh.castShadow = true;
          mesh.receiveShadow = true;
          mesh.name = `section_${index}`;
          houseGroup.add(mesh);
        }
      });
    }

    // Add roofs
    if (buildingGeometry.roofs && Array.isArray(buildingGeometry.roofs)) {
      buildingGeometry.roofs.forEach((roofData, index) => {
        const mesh = this.createMeshFromGeometry(roofData.geometry);
        if (mesh) {
          mesh.castShadow = true;
          mesh.receiveShadow = false; // Roofs don't receive shadows from above
          mesh.name = `roof_${index}`;
          houseGroup.add(mesh);
        }
        
        // Add parapet walls (for flat roofs with parapets - Modern/Brutalist styles)
        if (roofData.parapets && Array.isArray(roofData.parapets)) {
          roofData.parapets.forEach((parapetData, pIndex) => {
            const parapetMesh = this.createMeshFromGeometry(parapetData);
            if (parapetMesh) {
              parapetMesh.castShadow = true;
              parapetMesh.receiveShadow = true;
              parapetMesh.name = `parapet_${index}_${pIndex}`;
              houseGroup.add(parapetMesh);
            }
          });
        }
      });
    }

    // Add windows (if provided)
    if (buildingGeometry.windows && Array.isArray(buildingGeometry.windows)) {
      buildingGeometry.windows.forEach((windowData, index) => {
        const mesh = this.createMeshFromGeometry(windowData);
        if (mesh) {
          mesh.castShadow = false;
          mesh.receiveShadow = false;
          mesh.name = `window_${index}`;
          houseGroup.add(mesh);
        }
      });
    }

    // Add perforated wall face panels (ShapeGeometry with window/door holes)
    if (buildingGeometry.wallFaces && Array.isArray(buildingGeometry.wallFaces)) {
      buildingGeometry.wallFaces.forEach((faceData, index) => {
        const mesh = this.createWallFacePanel(faceData);
        if (mesh) {
          mesh.name = `wall_face_${index}`;
          houseGroup.add(mesh);
        }
      });
    }

    // Add interior walls (if provided)
    if (buildingGeometry.interiorWalls && Array.isArray(buildingGeometry.interiorWalls)) {
      buildingGeometry.interiorWalls.forEach((wallData, index) => {
        const mesh = this.createMeshFromGeometry(wallData);
        if (mesh) {
          mesh.castShadow = false;
          mesh.receiveShadow = true;
          mesh.name = `interior_wall_${index}`;
          houseGroup.add(mesh);
        }
      });
    }

    console.log(`Rendered building with ${houseGroup.children.length} meshes`);
  }
}
