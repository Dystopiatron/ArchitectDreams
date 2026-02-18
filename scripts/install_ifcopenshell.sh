#!/bin/bash
# IfcOpenShell Installation Script for Architectural Dream Machine
# This script sets up the Python environment for IFC post-processing

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
VENV_DIR="$PROJECT_DIR/ifcopenshell_env"

echo "=== IfcOpenShell Installation Script ==="
echo "Project directory: $PROJECT_DIR"
echo "Virtual environment: $VENV_DIR"
echo ""

# Check Python version
PYTHON_VERSION=$(python3 --version 2>&1)
echo "Found $PYTHON_VERSION"

# Create virtual environment if it doesn't exist
if [ ! -d "$VENV_DIR" ]; then
    echo "Creating virtual environment..."
    python3 -m venv "$VENV_DIR"
else
    echo "Virtual environment already exists"
fi

# Activate virtual environment
echo "Activating virtual environment..."
source "$VENV_DIR/bin/activate"

# Upgrade pip
echo "Upgrading pip..."
pip install --upgrade pip

# Install IfcOpenShell packages from pinned requirements
echo "Installing IfcOpenShell packages..."
pip install -r "$SCRIPT_DIR/requirements.txt"

# Verify installation
echo ""
echo "=== Verifying Installation ==="
python -c "import ifcopenshell; print(f'IfcOpenShell {ifcopenshell.version}')"
python -c "import ifctester; print('ifctester: OK')"
python -c "import ifcpatch; print('ifcpatch: OK')"
python -c "import ifcclash; print('ifcclash: OK')"

echo ""
echo "=== Installation Complete ==="
echo ""
echo "To activate the environment:"
echo "  source $VENV_DIR/bin/activate"
echo ""
echo "Available commands:"
echo "  python -m ifctester <ids_file> <ifc_file>  - Validate IFC against IDS"
echo "  python -m ifcpatch -i <input> -o <output> -r <recipe>  - Patch IFC files"
echo "  python -m ifcclash <clash_set.json>  - Detect clashes"
echo ""
