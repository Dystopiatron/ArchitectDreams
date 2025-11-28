# Architectural Dream Machine - Frontend

React Native iOS application for visualizing architectural designs in 3D.

## Quick Start

```bash
npm install
npx expo start --ios
```

## Configuration

### Backend API URL

Update `API_BASE_URL` in `screens/MainScreen.js`:

- **iOS Simulator**: `http://localhost:5162`
- **Physical Device**: `http://YOUR_MAC_IP:5162`

To find your Mac's IP:
```bash
ifconfig | grep "inet " | grep -v 127.0.0.1
```

## Features

- Input lot size and style prompt
- Real-time 3D model generation
- Interactive 3D viewer with Three.js
- Design parameters display
- Error handling and validation

## Project Structure

```
ArchitecturalDreamMachineFrontend/
├── screens/
│   └── MainScreen.js              # Main UI and 3D rendering
├── App.js                         # Navigation setup
├── app.json                       # Expo configuration
└── package.json                   # Dependencies
```

## Dependencies

- expo ~52.0.29
- react-native ~0.79.1
- @react-three/fiber
- three
- expo-gl
- axios
- @react-navigation/native
- @react-navigation/stack

## Usage

1. Enter lot size in square feet (e.g., 2500)
2. Enter style prompt (e.g., "Modern minimalist", "Victorian", "Brutalist")
3. Tap "Generate and Display Model"
4. View 3D model and design parameters

## Supported Styles

- **Modern**: Clean lines, large windows, flat roof
- **Victorian**: Ornate details, gabled roof, traditional
- **Brutalist**: Raw concrete, geometric, flat roof

## Testing

```bash
npm test
```

## Development Notes

- Uses @react-three/fiber for native 3D rendering
- Requires backend API to be running
- Network connectivity required for API calls
- 3D models are simple box geometry (can be enhanced)

## Troubleshooting

### Cannot connect to backend

1. Ensure backend is running (`dotnet run` in backend directory)
2. Check API_BASE_URL matches backend port
3. For physical device, use Mac's local IP instead of localhost

### 3D rendering issues

- Ensure expo-gl is properly installed
- Try restarting Expo dev server
- Check device/simulator OpenGL support

## Future Enhancements

- Cached design history
- Export to 3D file formats
- AR preview mode
- Material texture previews
- Multiple view angles
