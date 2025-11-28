/**
 * DisposalManager - Handles proper cleanup of Three.js resources
 * Prevents memory leaks by disposing geometries, materials, and textures
 */
export class DisposalManager {
  /**
   * Dispose all resources in a Three.js scene
   * @param {THREE.Scene} scene - Scene to clean up
   */
  static dispose(scene) {
    if (!scene) return;

    scene.traverse((object) => {
      // Dispose geometry
      if (object.geometry) {
        object.geometry.dispose();
      }

      // Dispose material(s)
      if (object.material) {
        if (Array.isArray(object.material)) {
          // Multiple materials
          object.material.forEach((material) => {
            this.disposeMaterial(material);
          });
        } else {
          // Single material
          this.disposeMaterial(object.material);
        }
      }
    });
  }

  /**
   * Dispose a single material and its textures
   * @param {THREE.Material} material - Material to dispose
   */
  static disposeMaterial(material) {
    if (!material) return;

    // Dispose all textures
    const textureProperties = [
      'map',
      'lightMap',
      'bumpMap',
      'normalMap',
      'specularMap',
      'envMap',
      'alphaMap',
      'aoMap',
      'displacementMap',
      'emissiveMap',
      'gradientMap',
      'metalnessMap',
      'roughnessMap'
    ];

    textureProperties.forEach((property) => {
      if (material[property] && material[property].dispose) {
        material[property].dispose();
      }
    });

    // Dispose the material itself
    material.dispose();
  }

  /**
   * Dispose a specific mesh and its resources
   * @param {THREE.Mesh} mesh - Mesh to dispose
   */
  static disposeMesh(mesh) {
    if (!mesh) return;

    if (mesh.geometry) {
      mesh.geometry.dispose();
    }

    if (mesh.material) {
      if (Array.isArray(mesh.material)) {
        mesh.material.forEach((material) => {
          this.disposeMaterial(material);
        });
      } else {
        this.disposeMaterial(mesh.material);
      }
    }
  }

  /**
   * Dispose a Three.js group and all its children
   * @param {THREE.Group} group - Group to dispose
   */
  static disposeGroup(group) {
    if (!group) return;

    while (group.children.length > 0) {
      const child = group.children[0];
      group.remove(child);
      
      if (child.geometry) {
        child.geometry.dispose();
      }
      
      if (child.material) {
        if (Array.isArray(child.material)) {
          child.material.forEach((material) => {
            this.disposeMaterial(material);
          });
        } else {
          this.disposeMaterial(child.material);
        }
      }
    }
  }
}
