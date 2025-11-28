import * as THREE from 'three';

/**
 * SceneManager - Handles Three.js scene initialization and management
 * Responsible for scene, camera, renderer, lighting, and ground setup
 */
export class SceneManager {
  constructor(mountElement, width, height) {
    this.mountElement = mountElement;
    this.width = width;
    this.height = height;
    
    // Initialize core Three.js components
    this.scene = new THREE.Scene();
    this.camera = new THREE.PerspectiveCamera(50, width / height, 0.1, 1000);
    this.renderer = new THREE.WebGLRenderer({ antialias: true });
    
    this._initScene();
    this._initRenderer();
    this._initLighting();
    this._initGround();
  }

  _initScene() {
    this.scene.background = new THREE.Color(0xf0f0f0);
  }

  _initRenderer() {
    this.renderer.setSize(this.width, this.height);
    this.renderer.shadowMap.enabled = true;
    
    // Configure canvas for touch scrolling
    const canvas = this.renderer.domElement;
    canvas.style.display = 'block';
    canvas.style.touchAction = 'pan-y'; // Allow vertical touch scrolling
    
    this.mountElement.appendChild(canvas);
  }

  _initLighting() {
    // Ambient light for overall illumination
    const ambientLight = new THREE.AmbientLight(0xffffff, 0.6);
    this.scene.add(ambientLight);

    // Directional light for shadows and depth
    const directionalLight = new THREE.DirectionalLight(0xffffff, 0.6);
    directionalLight.position.set(50, 100, 50);
    directionalLight.castShadow = true;
    
    // Shadow configuration
    directionalLight.shadow.mapSize.width = 2048;
    directionalLight.shadow.mapSize.height = 2048;
    directionalLight.shadow.camera.near = 0.5;
    directionalLight.shadow.camera.far = 500;
    directionalLight.shadow.camera.left = -100;
    directionalLight.shadow.camera.right = 100;
    directionalLight.shadow.camera.top = 100;
    directionalLight.shadow.camera.bottom = -100;
    
    this.scene.add(directionalLight);
  }

  _initGround() {
    // Ground plane
    const groundGeometry = new THREE.PlaneGeometry(200, 200);
    const groundMaterial = new THREE.MeshStandardMaterial({
      color: 0x90a080,
      roughness: 0.9,
      metalness: 0.0
    });
    
    const ground = new THREE.Mesh(groundGeometry, groundMaterial);
    ground.rotation.x = -Math.PI / 2;
    ground.position.y = 0;
    ground.receiveShadow = true;
    
    this.scene.add(ground);
  }

  /**
   * Position camera based on building dimensions
   * @param {number} maxDimension - Maximum building dimension for framing
   * @param {number} totalHeight - Total building height
   */
  positionCamera(maxDimension, totalHeight) {
    const distance = maxDimension * 1.8;
    const cameraHeight = totalHeight * 0.8;
    
    this.camera.position.set(distance, cameraHeight, distance);
    this.camera.lookAt(0, totalHeight / 2, 0);
  }

  /**
   * Render the scene
   */
  render() {
    this.renderer.render(this.scene, this.camera);
  }

  /**
   * Update renderer size
   * @param {number} width - New width
   * @param {number} height - New height
   */
  resize(width, height) {
    this.width = width;
    this.height = height;
    this.camera.aspect = width / height;
    this.camera.updateProjectionMatrix();
    this.renderer.setSize(width, height);
  }

  /**
   * Cleanup Three.js resources
   */
  dispose() {
    if (this.renderer) {
      this.renderer.dispose();
      if (this.mountElement && this.renderer.domElement) {
        this.mountElement.removeChild(this.renderer.domElement);
      }
    }
  }
}
