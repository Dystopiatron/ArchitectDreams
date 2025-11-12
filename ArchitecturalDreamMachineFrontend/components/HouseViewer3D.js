import React, { useEffect, useRef } from 'react';
import * as THREE from 'three';

export default function HouseViewer3D({ houseParams }) {
  const mountRef = useRef(null);
  const sceneRef = useRef(null);
  const rendererRef = useRef(null);
  const cameraRef = useRef(null);
  const meshRef = useRef(null);

  useEffect(() => {
    if (!mountRef.current || !houseParams) return;

    // Scene setup
    const scene = new THREE.Scene();
    scene.background = new THREE.Color(0xf0f0f0);
    sceneRef.current = scene;

    // Camera setup
    const width = mountRef.current.clientWidth;
    const canvasHeight = 400;
    const camera = new THREE.PerspectiveCamera(50, width / canvasHeight, 0.1, 1000);
    cameraRef.current = camera;

    // Renderer setup
    const renderer = new THREE.WebGLRenderer({ antialias: true });
    renderer.setSize(width, canvasHeight);
    renderer.shadowMap.enabled = true;
    
    // Add canvas and make it non-interactive for scrolling
    const canvas = renderer.domElement;
    canvas.style.display = 'block';
    canvas.style.touchAction = 'pan-y'; // Allow vertical touch scrolling
    
    mountRef.current.appendChild(canvas);
    rendererRef.current = renderer;

    // Lighting
    const ambientLight = new THREE.AmbientLight(0xffffff, 0.6);
    scene.add(ambientLight);

    const directionalLight = new THREE.DirectionalLight(0xffffff, 0.8);
    directionalLight.position.set(10, 20, 10);
    directionalLight.castShadow = true;
    scene.add(directionalLight);

    const directionalLight2 = new THREE.DirectionalLight(0xffffff, 0.4);
    directionalLight2.position.set(-10, 10, -10);
    scene.add(directionalLight2);

    // Ground plane
    const groundGeometry = new THREE.PlaneGeometry(200, 200);
    const groundMaterial = new THREE.MeshStandardMaterial({ 
      color: 0x7cb342,
      roughness: 0.8
    });
    const ground = new THREE.Mesh(groundGeometry, groundMaterial);
    ground.rotation.x = -Math.PI / 2;
    ground.position.y = -0.1;
    ground.receiveShadow = true;
    scene.add(ground);

    // House dimensions
    const baseSize = Math.sqrt(houseParams.lotSize);
    const height = baseSize * 0.6;

    // Color mapping
    const colorMap = {
      gray: 0x808080,
      cream: 0xfaf0dc,
      white: 0xffffff,
    };

    const color = colorMap[houseParams.material.color.toLowerCase()] || 0xffffff;

    // Create a group to hold all house parts
    const houseGroup = new THREE.Group();
    scene.add(houseGroup);
    meshRef.current = houseGroup;

    // Generate varied house layout based on style
    const layoutSeed = houseParams.lotSize % 5; // 0-4 for variety
    
    // Helper function to create a house section
    const createHouseSection = (width, height, depth, x, y, z, addWindows = true) => {
      const sectionGeometry = new THREE.BoxGeometry(width, height, depth);
      const sectionMaterial = new THREE.MeshStandardMaterial({ 
        color: color,
        roughness: 0.7,
        metalness: 0.1
      });
      const section = new THREE.Mesh(sectionGeometry, sectionMaterial);
      section.position.set(x, y, z);
      section.castShadow = true;
      section.receiveShadow = true;
      houseGroup.add(section);

      // Add windows to this section
      if (addWindows) {
        const windowMaterial = new THREE.MeshPhysicalMaterial({
          color: 0x87ceeb,
          transparent: true,
          opacity: 0.6,
          transmission: 0.5,
          roughness: 0.1,
          metalness: 0.1
        });

        const windowSize = houseParams.windowStyle === 'large' ? width * 0.12 : width * 0.08;
        const windowGeometry = new THREE.BoxGeometry(windowSize, windowSize, 0.5);

        // Front windows
        const numWindows = Math.floor(width / (windowSize * 2));
        for (let i = 0; i < Math.min(numWindows, 4); i++) {
          const window = new THREE.Mesh(windowGeometry, windowMaterial);
          const spacing = width / (numWindows + 1);
          window.position.set(
            x - width/2 + spacing * (i + 1),
            y + height * 0.3,
            z + depth / 2 + 0.3
          );
          houseGroup.add(window);
        }
      }

      return section;
    };

    // Generate different layouts based on seed
    let mainHeight = height;
    
    if (layoutSeed === 0) {
      // Traditional single-story cube
      createHouseSection(baseSize, height, baseSize, 0, height / 2, 0);
      
    } else if (layoutSeed === 1) {
      // Two-story house (Victorian common)
      const firstFloor = createHouseSection(baseSize, height * 0.6, baseSize, 0, height * 0.3, 0);
      const secondFloor = createHouseSection(baseSize * 0.8, height * 0.5, baseSize * 0.8, 0, height * 0.9, 0);
      mainHeight = height * 1.4;
      
    } else if (layoutSeed === 2) {
      // L-shaped house
      const mainWing = createHouseSection(baseSize, height, baseSize * 0.6, 0, height / 2, -baseSize * 0.2);
      const sideWing = createHouseSection(baseSize * 0.5, height, baseSize * 0.6, baseSize * 0.25, height / 2, baseSize * 0.2);
      
    } else if (layoutSeed === 3) {
      // Modern split-level
      const lowerLevel = createHouseSection(baseSize, height * 0.7, baseSize * 0.7, 0, height * 0.35, 0);
      const upperLevel = createHouseSection(baseSize * 0.6, height * 0.5, baseSize * 0.7, baseSize * 0.2, height * 0.85, 0);
      mainHeight = height * 1.2;
      
    } else {
      // Angled/rotated modern design
      const mainSection = createHouseSection(baseSize, height, baseSize * 0.7, 0, height / 2, 0);
      mainSection.rotation.y = Math.PI / 8; // 22.5 degree rotation
      
      const angleSection = createHouseSection(baseSize * 0.6, height * 0.8, baseSize * 0.5, baseSize * 0.3, height * 0.4, baseSize * 0.3);
      angleSection.rotation.y = -Math.PI / 6; // -30 degree rotation
    }

    // Add door (always on front)
    const doorGeometry = new THREE.BoxGeometry(baseSize * 0.08, baseSize * 0.18, 0.5);
    const doorMaterial = new THREE.MeshStandardMaterial({ 
      color: 0x654321,
      roughness: 0.8
    });
    const door = new THREE.Mesh(doorGeometry, doorMaterial);
    door.position.set(0, height * 0.09, baseSize / 2 + 0.3);
    houseGroup.add(door);

    // Roof based on layout and style
    let roof;
    if (houseParams.roofType === 'gabled') {
      // Gabled roof (peaked) - adapted for different layouts
      if (layoutSeed === 1) {
        // Two-story - smaller peaked roof on top
        const roofGeometry = new THREE.ConeGeometry(baseSize * 0.6, baseSize * 0.25, 4);
        const roofMaterial = new THREE.MeshStandardMaterial({ 
          color: 0x8b4513,
          roughness: 0.9
        });
        roof = new THREE.Mesh(roofGeometry, roofMaterial);
        roof.position.y = mainHeight + baseSize * 0.1;
        roof.rotation.y = Math.PI / 4;
        roof.castShadow = true;
      } else if (layoutSeed === 2) {
        // L-shaped - multiple roof sections
        const mainRoof = new THREE.ConeGeometry(baseSize * 0.5, baseSize * 0.25, 4);
        const roofMat = new THREE.MeshStandardMaterial({ color: 0x8b4513, roughness: 0.9 });
        roof = new THREE.Mesh(mainRoof, roofMat);
        roof.position.set(0, height + baseSize * 0.12, -baseSize * 0.2);
        roof.rotation.y = Math.PI / 4;
        roof.castShadow = true;
        
        const sideRoof = new THREE.Mesh(
          new THREE.ConeGeometry(baseSize * 0.4, baseSize * 0.2, 4),
          roofMat
        );
        sideRoof.position.set(baseSize * 0.25, height + baseSize * 0.1, baseSize * 0.2);
        sideRoof.rotation.y = Math.PI / 4;
        sideRoof.castShadow = true;
        houseGroup.add(sideRoof);
      } else if (layoutSeed === 4) {
        // Angled design - two separate rotated gabled roofs
        const roofMat = new THREE.MeshStandardMaterial({ color: 0x8b4513, roughness: 0.9 });
        
        // Main section roof (rotated 22.5 degrees) - sized to match building
        const mainRoof = new THREE.Mesh(
          new THREE.ConeGeometry(baseSize * 0.65, baseSize * 0.28, 4),
          roofMat
        );
        mainRoof.position.set(0, height + baseSize * 0.14, 0);
        mainRoof.rotation.y = Math.PI / 8 + Math.PI / 4; // Cone rotation + section rotation
        mainRoof.castShadow = true;
        houseGroup.add(mainRoof);
        
        // Angled section roof (rotated -30 degrees) - sized to match smaller building
        roof = new THREE.Mesh(
          new THREE.ConeGeometry(baseSize * 0.48, baseSize * 0.22, 4),
          roofMat
        );
        roof.position.set(baseSize * 0.3, height * 0.8 + baseSize * 0.11, baseSize * 0.3);
        roof.rotation.y = -Math.PI / 6 + Math.PI / 4; // Cone rotation + section rotation
        roof.castShadow = true;
      } else {
        // Standard peaked roof
        const roofGeometry = new THREE.ConeGeometry(baseSize * 0.7, baseSize * 0.3, 4);
        const roofMaterial = new THREE.MeshStandardMaterial({ 
          color: 0x8b4513,
          roughness: 0.9
        });
        roof = new THREE.Mesh(roofGeometry, roofMaterial);
        roof.position.y = mainHeight + baseSize * 0.1;
        roof.rotation.y = Math.PI / 4;
        roof.castShadow = true;
      }
    } else {
      // Flat roof - adapted for different layouts
      if (layoutSeed === 2) {
        // L-shaped flat roofs
        const mainRoof = new THREE.BoxGeometry(baseSize + 1, 0.8, baseSize * 0.6 + 1);
        const roofMat = new THREE.MeshStandardMaterial({ color: 0x555555, roughness: 0.8 });
        roof = new THREE.Mesh(mainRoof, roofMat);
        roof.position.set(0, height + 0.4, -baseSize * 0.2);
        roof.castShadow = true;
        
        const sideRoof = new THREE.Mesh(
          new THREE.BoxGeometry(baseSize * 0.5 + 1, 0.8, baseSize * 0.6 + 1),
          roofMat
        );
        sideRoof.position.set(baseSize * 0.25, height + 0.4, baseSize * 0.2);
        sideRoof.castShadow = true;
        houseGroup.add(sideRoof);
      } else if (layoutSeed === 4) {
        // Angled design - two separate rotated flat roofs
        const roofMat = new THREE.MeshStandardMaterial({ color: 0x555555, roughness: 0.8 });
        
        // Main section roof (rotated 22.5 degrees) - match building size
        const mainRoof = new THREE.Mesh(
          new THREE.BoxGeometry(baseSize + 2, 0.8, baseSize * 0.7 + 2),
          roofMat
        );
        mainRoof.position.set(0, height + 0.4, 0);
        mainRoof.rotation.y = Math.PI / 8; // Match main section rotation
        mainRoof.castShadow = true;
        houseGroup.add(mainRoof);
        
        // Angled section roof (rotated -30 degrees) - match building size
        roof = new THREE.Mesh(
          new THREE.BoxGeometry(baseSize * 0.6 + 2, 0.8, baseSize * 0.5 + 2),
          roofMat
        );
        roof.position.set(baseSize * 0.3, height * 0.8 + 0.4, baseSize * 0.3);
        roof.rotation.y = -Math.PI / 6; // Match angle section rotation
        roof.castShadow = true;
      } else {
        // Standard flat roof
        const roofGeometry = new THREE.BoxGeometry(baseSize + 2, 1, baseSize + 2);
        const roofMaterial = new THREE.MeshStandardMaterial({ 
          color: 0x555555,
          roughness: 0.8
        });
        roof = new THREE.Mesh(roofGeometry, roofMaterial);
        roof.position.y = mainHeight + 0.5;
        roof.castShadow = true;
      }
    }
    houseGroup.add(roof);

    // Camera position
    const distance = baseSize * 2.5; // Pull back more to see full house
    camera.position.set(distance, distance * 0.7, distance);
    camera.lookAt(0, mainHeight / 2, 0);

    // Animation
    let frameId;
    const animate = () => {
      frameId = requestAnimationFrame(animate);
      
      // Slowly rotate the entire house group
      if (meshRef.current) {
        meshRef.current.rotation.y += 0.003;
      }
      
      renderer.render(scene, camera);
    };
    animate();

    // Handle window resize
    const handleResize = () => {
      if (!mountRef.current) return;
      const width = mountRef.current.clientWidth;
      camera.aspect = width / canvasHeight;
      camera.updateProjectionMatrix();
      renderer.setSize(width, canvasHeight);
    };
    window.addEventListener('resize', handleResize);

    // Cleanup
    return () => {
      window.removeEventListener('resize', handleResize);
      cancelAnimationFrame(frameId);
      if (mountRef.current && renderer.domElement) {
        mountRef.current.removeChild(renderer.domElement);
      }
      renderer.dispose();
    };
  }, [houseParams]);

  if (!houseParams) {
    return null;
  }

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
        🔄 Auto-Rotating 3D Model • Layout varies by lot size
        <br />
        <span style={{ fontSize: '11px', color: '#999' }}>
          (Single-story, Two-story, L-shaped, Split-level, or Angled design)
        </span>
      </p>
    </div>
  );
}
