import React, { useEffect, useRef } from 'react';
import * as THREE from 'three';

export default function HouseViewer3D({ houseParams }) {
  const mountRef = useRef(null);
  const sceneRef = useRef(null);
  const rendererRef = useRef(null);
  const cameraRef = useRef(null);
  const meshRef = useRef(null);

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
    let handleResize;
    
    try {
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

        // Calculate building dimensions from lot size
    const stories = houseParams.stories || 1;
    const ceilingHeight = houseParams.ceilingHeight || 9.0;
    
    // Use footprint dimensions from backend (these match the room layout)
    const footprintWidth = houseParams.footprintWidth || Math.sqrt(houseParams.lotSize / stories / 1.5);
    const footprintDepth = houseParams.footprintDepth || (houseParams.lotSize / stories / footprintWidth);
    
    // Use footprintWidth as base size for compatibility with existing layouts
    const baseSize = footprintWidth;
    const baseDepth = footprintDepth;
    const height = ceilingHeight; // Single floor height
    const totalHeight = ceilingHeight * stories; // Total building height

    // Enhanced material mapping with architectural properties
    const colorMap = {
      gray: 0x808080,
      cream: 0xfaf0dc,
      white: 0xffffff,
    };

    const color = colorMap[houseParams.material.color.toLowerCase()] || 0xffffff;
    
    // Create materials based on exterior material type
    const exteriorMaterial = houseParams.exteriorMaterial || 'stucco';
    const roughnessMap = {
      'concrete': 0.95,
      'wood siding': 0.8,
      'stucco': 0.7,
      'brick': 0.85,
      'glass': 0.3
    };
    const roughness = roughnessMap[exteriorMaterial.toLowerCase()] || 0.7;

    // Create a group to hold all house parts
    const houseGroup = new THREE.Group();
    scene.add(houseGroup);
    meshRef.current = houseGroup;

    // Generate varied house layout based on style
    const layoutSeed = houseParams.lotSize % 5; // 0-4 for variety
    
    // Helper function to create a house section with architectural details
    const createHouseSection = (width, floorHeight, depth, x, y, z, addWindows = true, floorNum = 1) => {
      const sectionGeometry = new THREE.BoxGeometry(width, floorHeight, depth);
      const sectionMaterial = new THREE.MeshStandardMaterial({ 
        color: color,
        roughness: roughness,
        metalness: exteriorMaterial === 'concrete' ? 0.05 : 0.1
      });
      const section = new THREE.Mesh(sectionGeometry, sectionMaterial);
      section.position.set(x, y, z);
      section.castShadow = true;
      section.receiveShadow = true;
      houseGroup.add(section);

      // Add floor platform for upper stories
      if (floorNum > 1) {
        const floorGeometry = new THREE.BoxGeometry(width, 0.5, depth);
        const floorMaterial = new THREE.MeshStandardMaterial({
          color: 0x8b7355, // Wood color
          roughness: 0.8
        });
        const floor = new THREE.Mesh(floorGeometry, floorMaterial);
        floor.position.set(x, y - floorHeight/2 - 0.25, z);
        floor.castShadow = true;
        houseGroup.add(floor);
      }

      // Add windows to this section
      if (addWindows) {
        const windowMaterial = new THREE.MeshPhysicalMaterial({
          color: 0x88ccff,
          transparent: true,
          opacity: 0.7,
          transmission: 0.8,
          thickness: 0.5,
          roughness: 0.05,
          metalness: 0
        });

        // Window sizing based on style and window-to-wall ratio
        const windowRatio = houseParams.windowToWallRatio || 0.15;
        let windowWidth, windowHeight, numWindows;
        
        if (houseParams.windowStyle && houseParams.windowStyle.toLowerCase().includes('large')) {
          // Modern large windows
          windowWidth = width * 0.15;
          windowHeight = floorHeight * 0.6;
          numWindows = Math.floor(width / (windowWidth * 1.8));
        } else if (houseParams.windowStyle && houseParams.windowStyle.toLowerCase().includes('ornate')) {
          // Victorian smaller windows
          windowWidth = width * 0.08;
          windowHeight = floorHeight * 0.4;
          numWindows = Math.floor(width / (windowWidth * 1.5));
        } else {
          // Standard windows
          windowWidth = width * 0.10;
          windowHeight = floorHeight * 0.5;
          numWindows = Math.floor(width / (windowWidth * 2));
        }

        // Limit windows per wall
        numWindows = Math.min(numWindows, 5);
        numWindows = Math.max(numWindows, 1);

        // Front windows
        for (let i = 0; i < numWindows; i++) {
          const window = new THREE.Mesh(
            new THREE.BoxGeometry(windowWidth, windowHeight, 0.3),
            windowMaterial
          );
          const spacing = width / (numWindows + 1);
          window.position.set(
            x - width/2 + spacing * (i + 1),
            y,
            z + depth / 2 + 0.2
          );
          
          // Window frame
          const frameGeometry = new THREE.BoxGeometry(windowWidth * 1.1, windowHeight * 1.1, 0.2);
          const frameMaterial = new THREE.MeshStandardMaterial({
            color: 0xffffff,
            roughness: 0.4
          });
          const frame = new THREE.Mesh(frameGeometry, frameMaterial);
          frame.position.copy(window.position);
          frame.position.z -= 0.05;
          
          houseGroup.add(window);
          houseGroup.add(frame);
        }
      }

      return section;
    };

    // Create interior walls based on room layout
    const createInteriorWalls = (rooms) => {
      if (!rooms || rooms.length === 0) return;
      
      const wallThickness = 0.5; // 6 inches (standard interior wall)
      const wallMaterial = new THREE.MeshStandardMaterial({
        color: 0xf5f5dc, // Beige/cream color for interior walls
        roughness: 0.8,
        side: THREE.DoubleSide
      });
      
      // Backend generates rooms with coordinates from (0,0) origin
      // But Three.js building is centered at (0,0)
      // Need to offset room coordinates to center them
      const offsetX = -footprintWidth / 2;
      const offsetZ = -footprintDepth / 2;
      
      // Group rooms by floor for processing
      const roomsByFloor = {};
      rooms.forEach(room => {
        if (!roomsByFloor[room.floor]) {
          roomsByFloor[room.floor] = [];
        }
        roomsByFloor[room.floor].push(room);
      });
      
      // Create walls for each room
      Object.keys(roomsByFloor).forEach(floor => {
        const floorRooms = roomsByFloor[floor];
        const floorNum = parseInt(floor);
        const wallHeight = height;
        const floorY = floorNum === 1 ? 0 : (floorNum - 1) * height;
        
        floorRooms.forEach(room => {
          // Convert backend coordinates (origin at corner) to Three.js coordinates (origin at center)
          const roomCenterX = room.x + room.width / 2 + offsetX;
          const roomCenterZ = room.z + room.depth / 2 + offsetZ;
          
          // Room boundaries
          const roomMinX = roomCenterX - room.width / 2;
          const roomMaxX = roomCenterX + room.width / 2;
          const roomMinZ = roomCenterZ - room.depth / 2;
          const roomMaxZ = roomCenterZ + room.depth / 2;
          
          // Create 4 walls for each room
          // Only create walls that don't overlap with building exterior
          
          // Front wall (Z+)
          const frontWall = new THREE.Mesh(
            new THREE.BoxGeometry(room.width, wallHeight, wallThickness),
            wallMaterial
          );
          frontWall.position.set(roomCenterX, floorY + wallHeight / 2, roomMaxZ);
          
          // Back wall (Z-)
          const backWall = new THREE.Mesh(
            new THREE.BoxGeometry(room.width, wallHeight, wallThickness),
            wallMaterial
          );
          backWall.position.set(roomCenterX, floorY + wallHeight / 2, roomMinZ);
          
          // Left wall (X-)
          const leftWall = new THREE.Mesh(
            new THREE.BoxGeometry(wallThickness, wallHeight, room.depth),
            wallMaterial
          );
          leftWall.position.set(roomMinX, floorY + wallHeight / 2, roomCenterZ);
          
          // Right wall (X+)
          const rightWall = new THREE.Mesh(
            new THREE.BoxGeometry(wallThickness, wallHeight, room.depth),
            wallMaterial
          );
          rightWall.position.set(roomMaxX, floorY + wallHeight / 2, roomCenterZ);
          
          // Add doorway opening if room has a door (subtract a section from one wall)
          if (room.hasDoor) {
            const doorWidth = 3; // 36 inches standard
            const doorHeight = 6.67; // 80 inches
            
            // Create doorway in the front wall for simplicity
            // In future, could detect adjacent rooms and place doors between them
            const doorOpening = new THREE.Mesh(
              new THREE.BoxGeometry(doorWidth, doorHeight, wallThickness + 0.1),
              new THREE.MeshStandardMaterial({ 
                colorWrite: false, // Makes it invisible but still cuts geometry
                transparent: true,
                opacity: 0
              })
            );
            doorOpening.position.set(roomCenterX, floorY + doorHeight / 2, roomMaxZ);
            
            // Use CSG would be ideal here, but for simplicity we'll just mark the wall
            // For now, we'll skip adding the wall section where the door should be
          }
          
          // Only add interior walls (not on building perimeter)
          // Check if walls are internal by seeing if they're within building bounds
          const buildingHalfSize = baseSize / 2;
          
          if (Math.abs(roomMaxZ) < buildingHalfSize - 1) {
            houseGroup.add(frontWall);
          }
          if (Math.abs(roomMinZ) < buildingHalfSize - 1) {
            houseGroup.add(backWall);
          }
          if (Math.abs(roomMinX) < buildingHalfSize - 1) {
            houseGroup.add(leftWall);
          }
          if (Math.abs(roomMaxX) < buildingHalfSize - 1) {
            houseGroup.add(rightWall);
          }
        });
      });
    };

    // Generate different layouts based on seed
    let mainHeight = totalHeight;
    
    if (layoutSeed === 0) {
      // Traditional cube - support multi-story
      if (stories >= 2) {
        // First floor
        createHouseSection(baseSize, height, baseDepth, 0, height / 2, 0, true, 1);
        // Second floor
        createHouseSection(baseSize, height, baseDepth, 0, height + height / 2, 0, true, 2);
        mainHeight = height * 2;
      } else {
        createHouseSection(baseSize, height, baseDepth, 0, height / 2, 0);
        mainHeight = height;
      }
      
    } else if (layoutSeed === 1) {
      // Two-story house (always two stories for this layout)
      const actualStories = Math.max(stories, 2);
      const firstFloor = createHouseSection(baseSize, height, baseDepth, 0, height / 2, 0, true, 1);
      const secondFloor = createHouseSection(baseSize * 0.85, height, baseDepth * 0.85, 0, height + height / 2, 0, true, 2);
      mainHeight = height * 2;
      
    } else if (layoutSeed === 2) {
      // L-shaped house
      const mainWing = createHouseSection(baseSize, height, baseDepth * 0.6, 0, height / 2, -baseDepth * 0.2);
      const sideWing = createHouseSection(baseSize * 0.5, height, baseDepth * 0.6, baseSize * 0.25, height / 2, baseDepth * 0.2);
      mainHeight = height;
      
    } else if (layoutSeed === 3) {
      // Modern split-level
      const lowerLevel = createHouseSection(baseSize, height * 0.7, baseDepth * 0.7, 0, height * 0.35, 0);
      const upperLevel = createHouseSection(baseSize * 0.6, height * 0.5, baseDepth * 0.7, baseSize * 0.2, height * 0.85, 0);
      mainHeight = height * 1.2;
      
    } else {
      // Angled/rotated modern design
      const mainSection = createHouseSection(baseSize, height, baseDepth * 0.7, 0, height / 2, 0);
      mainSection.rotation.y = Math.PI / 8; // 22.5 degree rotation
      
      const angleSection = createHouseSection(baseSize * 0.6, height * 0.8, baseDepth * 0.5, baseSize * 0.3, height * 0.4, baseDepth * 0.3);
      angleSection.rotation.y = -Math.PI / 6; // -30 degree rotation
      mainHeight = height;
    }

    // Add entry door (always on front) with proper dimensions
    const doorWidth = 3; // 36 inches standard
    const doorHeight = 6.67; // 80 inches (6'8")
    const doorGeometry = new THREE.BoxGeometry(doorWidth, doorHeight, 0.3);
    const doorMaterial = new THREE.MeshStandardMaterial({ 
      color: 0x654321, // Brown wood
      roughness: 0.7,
      metalness: 0.1
    });
    const door = new THREE.Mesh(doorGeometry, doorMaterial);
    door.position.set(0, doorHeight / 2, baseDepth / 2 + 0.3);
    houseGroup.add(door);
    
    // Door frame
    const frameThickness = 0.3;
    const frameMaterial = new THREE.MeshStandardMaterial({
      color: 0xffffff,
      roughness: 0.4
    });
    
    // Top frame
    const topFrame = new THREE.Mesh(
      new THREE.BoxGeometry(doorWidth + frameThickness * 2, frameThickness, frameThickness),
      frameMaterial
    );
    topFrame.position.set(0, doorHeight + frameThickness / 2, baseDepth / 2 + 0.25);
    houseGroup.add(topFrame);
    
    // Foundation (if crawlspace)
    const foundationType = houseParams.foundationType || 'slab';
    if (foundationType === 'crawlspace') {
      const foundationHeight = 3; // 3 ft crawlspace
      const foundationGeometry = new THREE.BoxGeometry(baseSize + 1, foundationHeight, baseDepth + 1);
      const foundationMaterial = new THREE.MeshStandardMaterial({
        color: 0x555555, // Dark gray concrete
        roughness: 0.95
      });
      const foundation = new THREE.Mesh(foundationGeometry, foundationMaterial);
      foundation.position.y = -foundationHeight / 2;
      foundation.castShadow = true;
      houseGroup.add(foundation);
    }

    // Roof based on layout and style with architectural pitch
    let roof;
    const roofPitch = houseParams.roofPitch || 6; // Default 6:12 pitch
    const hasEaves = houseParams.hasEaves !== undefined ? houseParams.hasEaves : true;
    const overhang = hasEaves ? (houseParams.eavesOverhang || 1.5) : 0;
    
    if (houseParams.roofType === 'gabled') {
      // Calculate roof height from pitch (pitch/12 * half-width)
      const pitchRatio = roofPitch / 12;
      const roofHeight = (baseSize / 2) * pitchRatio;
      
      // Gabled roof (peaked) - adapted for different layouts
      if (layoutSeed === 1) {
        // Two-story - peaked roof on top with proper pitch
        const roofSize = baseSize * 0.85;
        const roofGeometry = new THREE.ConeGeometry(roofSize * 0.6 + overhang, roofHeight * 0.8, 4);
        const roofMaterial = new THREE.MeshStandardMaterial({ 
          color: 0x8b4513,
          roughness: 0.9
        });
        roof = new THREE.Mesh(roofGeometry, roofMaterial);
        roof.position.y = mainHeight + roofHeight * 0.4;
        roof.rotation.y = Math.PI / 4;
        roof.castShadow = true;
      } else if (layoutSeed === 2) {
        // L-shaped - multiple roof sections with proper pitch
        const mainRoof = new THREE.ConeGeometry(baseSize * 0.5 + overhang, roofHeight * 0.9, 4);
        const roofMat = new THREE.MeshStandardMaterial({ color: 0x8b4513, roughness: 0.9 });
        roof = new THREE.Mesh(mainRoof, roofMat);
        roof.position.set(0, mainHeight + roofHeight * 0.45, -baseSize * 0.2);
        roof.rotation.y = Math.PI / 4;
        roof.castShadow = true;
        
        const sideRoof = new THREE.Mesh(
          new THREE.ConeGeometry(baseSize * 0.4 + overhang, roofHeight * 0.7, 4),
          roofMat
        );
        sideRoof.position.set(baseSize * 0.25, mainHeight + roofHeight * 0.35, baseSize * 0.2);
        sideRoof.rotation.y = Math.PI / 4;
        sideRoof.castShadow = true;
        houseGroup.add(sideRoof);
      } else if (layoutSeed === 4) {
        // Angled design - two separate rotated gabled roofs with proper pitch
        const roofMat = new THREE.MeshStandardMaterial({ color: 0x8b4513, roughness: 0.9 });
        
        // Main section roof (rotated 22.5 degrees) - sized to match building
        const mainRoof = new THREE.Mesh(
          new THREE.ConeGeometry(baseSize * 0.65 + overhang, roofHeight, 4),
          roofMat
        );
        mainRoof.position.set(0, mainHeight + roofHeight * 0.5, 0);
        mainRoof.rotation.y = Math.PI / 8 + Math.PI / 4; // Cone rotation + section rotation
        mainRoof.castShadow = true;
        houseGroup.add(mainRoof);
        
        // Angled section roof (rotated -30 degrees) - sized to match smaller building
        roof = new THREE.Mesh(
          new THREE.ConeGeometry(baseSize * 0.48 + overhang, roofHeight * 0.8, 4),
          roofMat
        );
        roof.position.set(baseSize * 0.3, mainHeight * 0.8 + roofHeight * 0.4, baseSize * 0.3);
        roof.rotation.y = -Math.PI / 6 + Math.PI / 4; // Cone rotation + section rotation
        roof.castShadow = true;
      } else {
        // Standard peaked roof with architectural pitch
        const roofGeometry = new THREE.ConeGeometry(baseSize * 0.7 + overhang, roofHeight, 4);
        const roofMaterial = new THREE.MeshStandardMaterial({ 
          color: 0x8b4513,
          roughness: 0.9
        });
        roof = new THREE.Mesh(roofGeometry, roofMaterial);
        roof.position.y = mainHeight + roofHeight * 0.5;
        roof.rotation.y = Math.PI / 4;
        roof.castShadow = true;
      }
    } else {
      // Flat roof - adapted for different layouts
      const hasParapet = houseParams.hasParapet || false;
      const parapetHeight = hasParapet ? 2.5 : 0;
      
      if (layoutSeed === 2) {
        // L-shaped flat roofs
        const mainRoof = new THREE.BoxGeometry(baseSize + overhang * 2, 0.75, baseSize * 0.6 + overhang * 2);
        const roofMat = new THREE.MeshStandardMaterial({ color: 0x333333, roughness: 0.85 });
        roof = new THREE.Mesh(mainRoof, roofMat);
        roof.position.set(0, mainHeight + 0.375, -baseSize * 0.2);
        roof.castShadow = true;
        
        // Parapet for main section
        if (hasParapet) {
          const parapetGeometry = new THREE.BoxGeometry(baseSize + overhang * 2 + 0.5, parapetHeight, 0.5);
          const parapetMat = new THREE.MeshStandardMaterial({ color: 0xe0e0e0, roughness: 0.7 });
          
          ['front', 'back', 'left', 'right'].forEach(side => {
            const parapet = new THREE.Mesh(parapetGeometry, parapetMat);
            if (side === 'front') parapet.position.set(0, mainHeight + parapetHeight/2, -baseSize * 0.2 - baseSize * 0.3 - overhang);
            else if (side === 'back') parapet.position.set(0, mainHeight + parapetHeight/2, -baseSize * 0.2 + baseSize * 0.3 + overhang);
            houseGroup.add(parapet);
          });
        }
        
        const sideRoof = new THREE.Mesh(
          new THREE.BoxGeometry(baseSize * 0.5 + overhang * 2, 0.75, baseSize * 0.6 + overhang * 2),
          roofMat
        );
        sideRoof.position.set(baseSize * 0.25, mainHeight + 0.375, baseSize * 0.2);
        sideRoof.castShadow = true;
        houseGroup.add(sideRoof);
      } else if (layoutSeed === 4) {
        // Angled design - two separate rotated flat roofs with parapet
        const roofMat = new THREE.MeshStandardMaterial({ color: 0x333333, roughness: 0.85 });
        
        // Main section roof (rotated 22.5 degrees) - match building size
        const mainRoof = new THREE.Mesh(
          new THREE.BoxGeometry(baseSize + overhang * 2, 0.75, baseSize * 0.7 + overhang * 2),
          roofMat
        );
        mainRoof.position.set(0, mainHeight + 0.375, 0);
        mainRoof.rotation.y = Math.PI / 8; // Match main section rotation
        mainRoof.castShadow = true;
        houseGroup.add(mainRoof);
        
        // Angled section roof (rotated -30 degrees) - match building size
        roof = new THREE.Mesh(
          new THREE.BoxGeometry(baseSize * 0.6 + overhang * 2, 0.75, baseSize * 0.5 + overhang * 2),
          roofMat
        );
        roof.position.set(baseSize * 0.3, mainHeight * 0.8 + 0.375, baseSize * 0.3);
        roof.rotation.y = -Math.PI / 6; // Match angle section rotation
        roof.castShadow = true;
      } else {
        // Standard flat roof with parapet
        const roofGeometry = new THREE.BoxGeometry(baseSize + overhang * 2, 0.75, baseSize + overhang * 2);
        const roofMaterial = new THREE.MeshStandardMaterial({ 
          color: 0x333333,
          roughness: 0.85
        });
        roof = new THREE.Mesh(roofGeometry, roofMaterial);
        roof.position.y = mainHeight + 0.375;
        roof.castShadow = true;
        
        // Add parapet (low wall around perimeter) for modern flat roofs
        if (hasParapet) {
          const parapetMaterial = new THREE.MeshStandardMaterial({
            color: 0xe0e0e0,
            roughness: 0.7
          });
          
          const parapetThickness = 0.5;
          
          // Front parapet
          const frontParapet = new THREE.Mesh(
            new THREE.BoxGeometry(baseSize + overhang * 2, parapetHeight, parapetThickness),
            parapetMaterial
          );
          frontParapet.position.set(0, mainHeight + parapetHeight / 2, baseSize / 2 + overhang);
          houseGroup.add(frontParapet);
          
          // Back parapet
          const backParapet = new THREE.Mesh(
            new THREE.BoxGeometry(baseSize + overhang * 2, parapetHeight, parapetThickness),
            parapetMaterial
          );
          backParapet.position.set(0, mainHeight + parapetHeight / 2, -baseSize / 2 - overhang);
          houseGroup.add(backParapet);
          
          // Left parapet
          const leftParapet = new THREE.Mesh(
            new THREE.BoxGeometry(parapetThickness, parapetHeight, baseSize + overhang * 2),
            parapetMaterial
          );
          leftParapet.position.set(-baseSize / 2 - overhang, mainHeight + parapetHeight / 2, 0);
          houseGroup.add(leftParapet);
          
          // Right parapet
          const rightParapet = new THREE.Mesh(
            new THREE.BoxGeometry(parapetThickness, parapetHeight, baseSize + overhang * 2),
            parapetMaterial
          );
          rightParapet.position.set(baseSize / 2 + overhang, mainHeight + parapetHeight / 2, 0);
          houseGroup.add(rightParapet);
        }
      }
    }
    if (roof) houseGroup.add(roof);

    // Add interior walls based on room layout
    if (houseParams.rooms && houseParams.rooms.length > 0) {
      console.log('Creating interior walls for', houseParams.rooms.length, 'rooms');
      createInteriorWalls(houseParams.rooms);
    }

    // Camera position - adjusted for building size and height
    const roofPeakHeight = houseParams.roofType === 'gabled' ? 
      (baseSize / 2) * (roofPitch / 12) : 0;
    const buildingTopHeight = mainHeight + roofPeakHeight;
    
    const distance = Math.max(baseSize * 2.2, 60); // Ensure minimum viewing distance
    camera.position.set(distance, buildingTopHeight * 0.8, distance);
    camera.lookAt(0, buildingTopHeight * 0.4, 0);

    // Animation
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
    handleResize = () => {
      if (!mountRef.current) return;
      const width = mountRef.current.clientWidth;
      camera.aspect = width / canvasHeight;
      camera.updateProjectionMatrix();
      renderer.setSize(width, canvasHeight);
    };
    window.addEventListener('resize', handleResize);

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
    
    // Cleanup (outside try-catch)
    return () => {
      window.removeEventListener('resize', handleResize);
      if (frameId) cancelAnimationFrame(frameId);
      if (mountRef.current && rendererRef.current?.domElement) {
        mountRef.current.removeChild(rendererRef.current.domElement);
      }
      if (rendererRef.current) rendererRef.current.dispose();
    };
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
