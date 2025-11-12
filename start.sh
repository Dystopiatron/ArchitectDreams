#!/bin/bash

echo "🏗️  Architectural Dream Machine - Quick Start"
echo ""

# Check prerequisites
echo "Checking prerequisites..."

if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK not found. Please install .NET 8 SDK"
    exit 1
fi

if ! command -v node &> /dev/null; then
    echo "❌ Node.js not found. Please install Node.js 18+"
    exit 1
fi

echo "✅ Prerequisites OK"
echo ""

# Start backend
echo "🚀 Starting backend API..."
cd ArchitecturalDreamMachineBackend/ArchitecturalDreamMachineBackend

# Build and run in background
dotnet build
if [ $? -ne 0 ]; then
    echo "❌ Backend build failed"
    exit 1
fi

echo "Starting backend server..."
dotnet run &
BACKEND_PID=$!

echo "Backend running with PID: $BACKEND_PID"
echo "Waiting for backend to start..."
sleep 5

# Get backend URL
echo ""
echo "📡 Backend API should be running at http://localhost:5162"
echo "   Visit http://localhost:5162/swagger for API docs"
echo ""

# Provide frontend instructions
echo "📱 To start the frontend:"
echo "   1. Open a new terminal"
echo "   2. cd ArchitecturalDreamMachineFrontend"
echo "   3. npm install (if not done)"
echo "   4. npx expo start --ios"
echo ""

echo "To stop the backend, run: kill $BACKEND_PID"
echo ""
echo "✨ Setup complete!"
