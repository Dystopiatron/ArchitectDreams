import React, { useEffect, useRef } from 'react';
import * as THREE from 'three';
import { SceneManager } from '../renderers/SceneManager.js';
import { GeometryRenderer } from '../renderers/GeometryRenderer.js';
import { DisposalManager } from '../utils/DisposalManager.js';

export default function HouseViewer3D({ houseParams }) {
  const mountRef = useRef(null);
  const sceneManagerRef = useRef(null);
  const houseGroupRef = useRef(null);

  useEffect(() => {
    if (!mountRef.current || !houseParams) {
      console.log('HouseViewer3D: Missing mount or params', { 
        hasMount: !!mountRef.current, 
        hasParams: !!houseParams 
      });
      return;
    }
    
    console.log('HouseViewer3D: Initializing with params', houseParams);

    let frameId;
    
    try {
      // Initialize scene manager
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
        console.log('✅ Backend geometry received:', {
          sections: houseParams.geometry.sections?.length || 0,
          roofs: houseParams.geometry.roofs?.length || 0,
          totalHeight: houseParams.geometry.totalHeight,
          maxDimension: houseParams.geometry.maxDimension
        });
        GeometryRenderer.renderBuilding(houseGroup, houseParams.geometry);
        
        // Position camera based on backend dimensions
        sceneManager.positionCamera(
          houseParams.geometry.maxDimension || 50,
          houseParams.geometry.totalHeight || 20
        );
      } else {
        console.warn('No geometry provided by backend, falling back to legacy mesh');
        // FALLBACK: Use legacy mesh if backend doesn't provide geometry yet
        if (houseParams.mesh) {
          const legacyMesh = createLegacyMesh(houseParams.mesh);
          if (legacyMesh) {
            houseGroup.add(legacyMesh);
          }
        }
        
        // Position camera for legacy mesh
        const buildingSize = Math.sqrt(houseParams.lotSize);
        sceneManager.positionCamera(buildingSize, buildingSize * 0.5);
      }
      
      // Animation loop
      const animate = () => {
        frameId = requestAnimationFrame(animate);
        houseGroup.rotation.y += 0.003;
        sceneManager.render();
      };
      animate();
      
      // Resize handler
      const handleResize = () => {
        if (!mountRef.current) return;
        const width = mountRef.current.clientWidth;
        sceneManager.resize(width, 400);
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
      console.error('HouseViewer3D: Error during initialization', error);
      // Display error in the component
      if (mountRef.current) {
        mountRef.current.innerHTML = `
          <div style="padding: 20px; background: #ffebee; border-radius: 8px; color: #c62828;">
            <h3>3D Viewer Error</h3>
            <p>${error.message}</p>
            <pre style="font-size: 11px; overflow: auto;">${error.stack}</pre>
          </div>
        `;
      }
    }
  }, [houseParams]);

  if (!houseParams) {
    return null;
  }

  // Calculate display values from houseParams
  const displayStories = houseParams.stories || 1;
  const displayRoofPitch = houseParams.roofPitch || 6;
  const displayCeilingHeight = houseParams.ceilingHeight || 9.0;
  const displayFoundationType = houseParams.foundationType || 'slab';
  
  // Calculate footprint for display
  const desiredBuildingSqFt = houseParams.lotSize;
  const footprintSqFt = desiredBuildingSqFt / displayStories;
  const aspectRatio = 1.5;
  const displayFootprintWidth = Math.sqrt(footprintSqFt / aspectRatio);
  const displayFootprintDepth = footprintSqFt / displayFootprintWidth;

  return (
    <div>
      <div 
        ref={mountRef} 
        style={{ 
          width: '100%', 
          height: '400px',
          borderRadius: '8px',
          overflow: 'hidden'
        }} 
      />
      <p style={{ 
        textAlign: 'center', 
        fontSize: '12px', 
        color: '#666',
        marginTop: '8px'
      }}>
        🔄 Auto-Rotating 3D Model • {displayStories}-Story • {houseParams.roofType} roof
        {houseParams.roofType === 'gabled' && ` (${displayRoofPitch}:12 pitch)`}
        <br />
        <span style={{ fontSize: '11px', color: '#999' }}>
          {Math.round(displayFootprintWidth)}×{Math.round(displayFootprintDepth)} ft • 
          {displayCeilingHeight} ft ceilings • 
          {displayFoundationType} foundation
        </span>
      </p>
    </div>
  );
}

/**
 * Legacy mesh renderer - TEMPORARY fallback for gradual migration
 */
function createLegacyMesh(meshData) {
  if (!meshData || !meshData.vertices || !meshData.faces) {
    console.warn('Invalid legacy mesh data');
    return null;
  }

  try {
    const geometry = new THREE.BufferGeometry();
    
    // Convert vertices
    const vertices = new Float32Array(meshData.vertices.length * 3);
    meshData.vertices.forEach((vertex, i) => {
      vertices[i * 3] = vertex.x;
      vertices[i * 3 + 1] = vertex.y;
      vertices[i * 3 + 2] = vertex.z;
    });
    geometry.setAttribute('position', new THREE.BufferAttribute(vertices, 3));
    
    // Convert faces
    const indices = [];
    meshData.faces.forEach(face => {
      indices.push(face.a, face.b, face.c);
    });
    geometry.setIndex(indices);
    geometry.computeVertexNormals();
    
    // Create material
    const material = new THREE.MeshStandardMaterial({
      color: meshData.color || 0xffffff,
      roughness: 0.7,
      side: THREE.DoubleSide
    });
    
    return new THREE.Mesh(geometry, material);
  } catch (error) {
    console.error('Error creating legacy mesh:', error);
    return null;
  }
}
