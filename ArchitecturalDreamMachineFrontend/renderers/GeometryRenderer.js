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
      console.error('❌ Invalid geometry data:', geometryData);
      return null;
    }

    console.log('🔨 Creating mesh:', {
      vertexCount: geometryData.vertices.length / 3,
      faceCount: geometryData.indices.length / 3,
      material: geometryData.materialType,
      color: geometryData.color
    });

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
        return new THREE.MeshStandardMaterial({
          ...materialConfig,
          roughness: 0.7,
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
