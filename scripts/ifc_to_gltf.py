#!/usr/bin/env python3
"""
IFC to glTF Converter for Architectural Dream Machine
Uses IfcOpenShell's geometry processing API

Usage:
    python ifc_to_gltf.py input.ifc output.glb
"""

import sys
import os
import ifcopenshell
import ifcopenshell.geom

def convert_ifc_to_obj(input_path: str, output_path: str) -> bool:
    """
    Convert IFC file to OBJ format using IfcOpenShell geometry API.
    
    Note: For glTF/GLB output, the IfcConvert binary is recommended.
    Download from: https://blenderbim.org/docs-python/ifcconvert/installation.html
    
    This script provides OBJ output as an alternative.
    
    Args:
        input_path: Path to input IFC file
        output_path: Path to output file (OBJ or GLB)
        
    Returns:
        True if conversion successful, False otherwise
    """
    try:
        print(f"Loading IFC file: {input_path}")
        model = ifcopenshell.open(input_path)
        
        # Configure geometry settings
        settings = ifcopenshell.geom.settings()
        settings.set(settings.USE_WORLD_COORDS, True)
        
        # Get all products with geometry
        products = model.by_type("IfcProduct")
        
        vertices = []
        faces = []
        vertex_offset = 0
        
        print(f"Processing {len(products)} products...")
        
        for product in products:
            try:
                # Create shape for the product
                shape = ifcopenshell.geom.create_shape(settings, product)
                
                # Get geometry data
                geometry = shape.geometry
                verts = geometry.verts
                face_indices = geometry.faces
                
                # Add vertices (grouped by 3: x, y, z)
                for i in range(0, len(verts), 3):
                    vertices.append((verts[i], verts[i+1], verts[i+2]))
                
                # Add faces (grouped by 3: triangle indices)
                for i in range(0, len(face_indices), 3):
                    faces.append((
                        face_indices[i] + vertex_offset + 1,
                        face_indices[i+1] + vertex_offset + 1,
                        face_indices[i+2] + vertex_offset + 1
                    ))
                
                vertex_offset += len(verts) // 3
                
            except Exception as e:
                # Skip products without geometry
                continue
        
        # Write OBJ file
        print(f"Writing OBJ file: {output_path}")
        with open(output_path, 'w') as f:
            f.write(f"# Converted from {os.path.basename(input_path)}\n")
            f.write(f"# Vertices: {len(vertices)}\n")
            f.write(f"# Faces: {len(faces)}\n\n")
            
            for v in vertices:
                f.write(f"v {v[0]} {v[1]} {v[2]}\n")
            
            f.write("\n")
            
            for face in faces:
                f.write(f"f {face[0]} {face[1]} {face[2]}\n")
        
        print(f"Conversion complete: {len(vertices)} vertices, {len(faces)} faces")
        return True
        
    except Exception as e:
        print(f"Error: {e}")
        return False


def main():
    if len(sys.argv) < 3:
        print(__doc__)
        print("\nNote: For GLB/glTF output, download the IfcConvert binary from:")
        print("  https://blenderbim.org/docs-python/ifcconvert/installation.html")
        print("\nThis script outputs OBJ format.")
        sys.exit(1)
    
    input_path = sys.argv[1]
    output_path = sys.argv[2]
    
    if not os.path.exists(input_path):
        print(f"Error: Input file not found: {input_path}")
        sys.exit(1)
    
    # Change extension to .obj if not already
    if output_path.endswith('.glb') or output_path.endswith('.gltf'):
        output_path = output_path.rsplit('.', 1)[0] + '.obj'
        print(f"Note: Converting to OBJ format: {output_path}")
    
    success = convert_ifc_to_obj(input_path, output_path)
    sys.exit(0 if success else 1)


if __name__ == "__main__":
    main()
