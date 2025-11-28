import React, { useEffect, useRef, useState } from 'react';
import * as THREE from 'three';
import { SceneManager } from '../renderers/SceneManager.js';
import { GeometryRenderer } from '../renderers/GeometryRenderer.js';
import { DisposalManager } from '../utils/DisposalManager.js';
import { CameraController } from '../utils/CameraController.js';

export default function HouseViewer3D({ houseParams }) {
  const mountRef = useRef(null);
  const sceneManagerRef = useRef(null);
  const houseGroupRef = useRef(null);
  const cameraControllerRef = useRef(null);
  const [autoRotate, setAutoRotate] = useState(true);

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
        const targetHeight = houseParams.geometry.totalHeight / 2;
        sceneManager.positionCamera(
          houseParams.geometry.maxDimension || 50,
          houseParams.geometry.totalHeight || 20
        );
        
        // Initialize camera controller
        cameraControllerRef.current = new CameraController(
          sceneManager.camera,
          { x: 0, y: targetHeight, z: 0 }
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
        
        // Initialize camera controller for legacy
        cameraControllerRef.current = new CameraController(
          sceneManager.camera,
          { x: 0, y: buildingSize * 0.25, z: 0 }
        );
      }
      
      // Mouse/touch controls for camera
      const canvas = sceneManager.renderer.domElement;
      let isDragging = false;
      let previousMousePosition = { x: 0, y: 0 };
      
      const onMouseDown = (e) => {
        isDragging = true;
        setAutoRotate(false);
        previousMousePosition = {
          x: e.clientX || e.touches?.[0]?.clientX,
          y: e.clientY || e.touches?.[0]?.clientY
        };
      };
      
      const onMouseMove = (e) => {
        if (!isDragging || !cameraControllerRef.current) return;
        
        const currentX = e.clientX || e.touches?.[0]?.clientX;
        const currentY = e.clientY || e.touches?.[0]?.clientY;
        
        const deltaX = currentX - previousMousePosition.x;
        const deltaY = currentY - previousMousePosition.y;
        
        // Rotate camera based on mouse movement
        cameraControllerRef.current.rotateHorizontal(-deltaX * 0.01);
        cameraControllerRef.current.rotateVertical(deltaY * 0.01);
        
        previousMousePosition = { x: currentX, y: currentY };
      };
      
      const onMouseUp = () => {
        isDragging = false;
      };
      
      const onWheel = (e) => {
        e.preventDefault();
        if (!cameraControllerRef.current) return;
        
        // Zoom based on wheel delta
        const zoomSpeed = 2;
        cameraControllerRef.current.zoom(e.deltaY * 0.01 * zoomSpeed);
      };
      
      // Add event listeners
      canvas.addEventListener('mousedown', onMouseDown);
      canvas.addEventListener('mousemove', onMouseMove);
      canvas.addEventListener('mouseup', onMouseUp);
      canvas.addEventListener('mouseleave', onMouseUp);
      canvas.addEventListener('touchstart', onMouseDown);
      canvas.addEventListener('touchmove', onMouseMove);
      canvas.addEventListener('touchend', onMouseUp);
      canvas.addEventListener('wheel', onWheel, { passive: false });
      
      // Animation loop
      const animate = () => {
        frameId = requestAnimationFrame(animate);
        
        // Auto-rotate if enabled
        if (autoRotate) {
          houseGroup.rotation.y += 0.003;
        }
        
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
        
        // Remove mouse/touch listeners
        const canvas = sceneManager.renderer.domElement;
        canvas.removeEventListener('mousedown', onMouseDown);
        canvas.removeEventListener('mousemove', onMouseMove);
        canvas.removeEventListener('mouseup', onMouseUp);
        canvas.removeEventListener('mouseleave', onMouseUp);
        canvas.removeEventListener('touchstart', onMouseDown);
        canvas.removeEventListener('touchmove', onMouseMove);
        canvas.removeEventListener('touchend', onMouseUp);
        canvas.removeEventListener('wheel', onWheel);
        
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

  // Camera control handlers
  const handleViewChange = (viewType) => {
    if (!cameraControllerRef.current) return;
    setAutoRotate(false);
    
    switch(viewType) {
      case 'front': cameraControllerRef.current.setFrontView(); break;
      case 'back': cameraControllerRef.current.setBackView(); break;
      case 'left': cameraControllerRef.current.setLeftView(); break;
      case 'right': cameraControllerRef.current.setRightView(); break;
      case 'top': cameraControllerRef.current.setTopView(); break;
      case 'iso': cameraControllerRef.current.setIsometricView(); break;
      case 'reset': 
        cameraControllerRef.current.reset();
        setAutoRotate(true);
        break;
    }
  };

  return (
    <div>
      <div style={{ position: 'relative', width: '100%' }}>
        <div 
          ref={mountRef} 
          style={{ 
            width: '100%', 
            height: '400px',
            borderRadius: '8px',
            overflow: 'hidden'
          }} 
        />
        
        {/* Camera Controls Overlay */}
        <div style={{
          position: 'absolute',
          top: 10,
          right: 10,
          display: 'flex',
          flexDirection: 'column',
          gap: 5,
          zIndex: 10
        }}>
          <button onClick={() => handleViewChange('front')} style={controlButtonStyle} title="Front View">
            Front
          </button>
          <button onClick={() => handleViewChange('back')} style={controlButtonStyle} title="Back View">
            Back
          </button>
          <button onClick={() => handleViewChange('left')} style={controlButtonStyle} title="Left View">
            Left
          </button>
          <button onClick={() => handleViewChange('right')} style={controlButtonStyle} title="Right View">
            Right
          </button>
          <button onClick={() => handleViewChange('top')} style={controlButtonStyle} title="Top View">
            Top
          </button>
          <button onClick={() => handleViewChange('iso')} style={controlButtonStyle} title="Isometric View">
            ISO
          </button>
          <button onClick={() => handleViewChange('reset')} style={{...controlButtonStyle, backgroundColor: '#4caf50'}} title="Reset to Auto-Rotate">
            Reset
          </button>
        </div>
        
        {/* Instructions */}
        <div style={{
          position: 'absolute',
          bottom: 10,
          left: 10,
          backgroundColor: 'rgba(42, 42, 42, 0.9)',
          color: '#e0e0e0',
          padding: '8px 12px',
          borderRadius: 6,
          fontSize: 12,
          zIndex: 10
        }}>
          🖱️ Drag to rotate • Scroll to zoom • Click buttons for preset views
        </div>
      </div>
      
      <p style={{ 
        textAlign: 'center', 
        fontSize: '12px', 
        color: '#b0b0b0',
        marginTop: '8px'
      }}>
        {autoRotate ? '🔄 Auto-Rotating' : '🎮 Manual Control'} • {displayStories}-Story • {houseParams.roofType} roof
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

// Button style for camera controls
const controlButtonStyle = {
  backgroundColor: 'rgba(42, 42, 42, 0.9)',
  color: '#e0e0e0',
  border: '1px solid #444',
  borderRadius: 6,
  padding: '8px 12px',
  fontSize: 12,
  fontWeight: '600',
  cursor: 'pointer',
  transition: 'all 0.2s',
  minWidth: 60,
  textAlign: 'center'
};
