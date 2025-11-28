/**
 * CameraController - Manual camera controls for 3D viewer
 * Provides zoom, orbit, and preset view angles
 */
export class CameraController {
  constructor(camera, targetPosition = { x: 0, y: 10, z: 0 }) {
    this.camera = camera;
    this.target = targetPosition;
    
    // Store initial camera state
    this.defaultDistance = Math.sqrt(
      Math.pow(camera.position.x, 2) +
      Math.pow(camera.position.y - targetPosition.y, 2) +
      Math.pow(camera.position.z, 2)
    );
    
    // Camera state
    this.distance = this.defaultDistance;
    this.azimuth = Math.atan2(camera.position.x, camera.position.z); // Horizontal angle
    this.elevation = Math.atan2(camera.position.y - targetPosition.y, 
      Math.sqrt(Math.pow(camera.position.x, 2) + Math.pow(camera.position.z, 2))
    ); // Vertical angle
    
    // Limits
    this.minDistance = this.defaultDistance * 0.3;
    this.maxDistance = this.defaultDistance * 3;
    this.minElevation = 0.1; // Prevent looking directly from below
    this.maxElevation = Math.PI / 2 - 0.1; // Prevent looking directly from above
  }

  /**
   * Zoom in/out
   * @param {number} delta - Positive to zoom in, negative to zoom out
   */
  zoom(delta) {
    this.distance = Math.max(
      this.minDistance,
      Math.min(this.maxDistance, this.distance + delta)
    );
    this.updateCameraPosition();
  }

  /**
   * Rotate camera horizontally (orbit)
   * @param {number} delta - Angle in radians
   */
  rotateHorizontal(delta) {
    this.azimuth += delta;
    this.updateCameraPosition();
  }

  /**
   * Rotate camera vertically (elevation)
   * @param {number} delta - Angle in radians
   */
  rotateVertical(delta) {
    this.elevation = Math.max(
      this.minElevation,
      Math.min(this.maxElevation, this.elevation + delta)
    );
    this.updateCameraPosition();
  }

  /**
   * Update camera position based on spherical coordinates
   */
  updateCameraPosition() {
    // Convert spherical coordinates to Cartesian
    this.camera.position.x = this.distance * Math.sin(this.elevation) * Math.sin(this.azimuth);
    this.camera.position.y = this.target.y + this.distance * Math.cos(this.elevation);
    this.camera.position.z = this.distance * Math.sin(this.elevation) * Math.cos(this.azimuth);
    
    this.camera.lookAt(this.target.x, this.target.y, this.target.z);
  }

  /**
   * Reset to default view
   */
  reset() {
    this.distance = this.defaultDistance;
    this.azimuth = Math.PI / 4; // 45 degrees
    this.elevation = Math.PI / 3; // ~60 degrees
    this.updateCameraPosition();
  }

  /**
   * Preset views
   */
  setFrontView() {
    this.azimuth = 0;
    this.elevation = Math.PI / 4;
    this.updateCameraPosition();
  }

  setBackView() {
    this.azimuth = Math.PI;
    this.elevation = Math.PI / 4;
    this.updateCameraPosition();
  }

  setLeftView() {
    this.azimuth = -Math.PI / 2;
    this.elevation = Math.PI / 4;
    this.updateCameraPosition();
  }

  setRightView() {
    this.azimuth = Math.PI / 2;
    this.elevation = Math.PI / 4;
    this.updateCameraPosition();
  }

  setTopView() {
    this.elevation = Math.PI / 2 - 0.1;
    this.updateCameraPosition();
  }

  setIsometricView() {
    this.azimuth = Math.PI / 4;
    this.elevation = Math.PI / 3;
    this.updateCameraPosition();
  }
}
