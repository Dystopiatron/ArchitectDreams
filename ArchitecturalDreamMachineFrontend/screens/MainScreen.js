import React, { useState } from 'react';
import {
  View,
  Text,
  TextInput,
  TouchableOpacity,
  StyleSheet,
  ScrollView,
  Platform,
} from 'react-native';
import axios from 'axios';
import HouseViewer3D from '../components/HouseViewer3D';

// Replace with your local IP if testing on physical device
const API_BASE_URL = 'http://localhost:5095';

export default function MainScreen() {
  const [lotSize, setLotSize] = useState('');
  const [stylePrompt, setStylePrompt] = useState('');
  const [layoutType, setLayoutType] = useState('auto'); // auto, cube, two-story, l-shape, split, angled
  const [stories, setStories] = useState('auto'); // auto, 1, 2, 3
  const [errorMessage, setErrorMessage] = useState('');
  const [houseParams, setHouseParams] = useState(null);
  const [isLoading, setIsLoading] = useState(false);
  const [designInfo, setDesignInfo] = useState(null);
  const [meshData, setMeshData] = useState(null);

  const handleDownloadOBJ = async () => {
    if (!designInfo?.designId) return;

    try {
      const response = await axios.get(
        `${API_BASE_URL}/api/designs/${designInfo.designId}/export`,
        { responseType: 'blob' }
      );

      // Create download link
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', `house_design_${designInfo.designId}.obj`);
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
    } catch (error) {
      console.error('Error downloading OBJ:', error);
      setErrorMessage('Failed to download OBJ file');
    }
  };

  const handleGenerate = async () => {
    // Clear previous errors
    setErrorMessage('');
    
    // Validate inputs
    const lotSizeNum = parseFloat(lotSize);
    if (!lotSize || isNaN(lotSizeNum) || lotSizeNum <= 0) {
      setErrorMessage('Please enter a valid lot size greater than 0');
      return;
    }

    if (!stylePrompt.trim()) {
      setErrorMessage('Please enter a style prompt');
      return;
    }

    setIsLoading(true);

    try {
      // Build request with optional overrides
      const requestData = {
        lotSize: lotSizeNum,
        stylePrompt: stylePrompt.trim(),
      };
      
      // Add overrides if user specified them
      if (layoutType !== 'auto') {
        requestData.buildingShapeOverride = layoutType;
      }
      if (stories !== 'auto') {
        requestData.storiesOverride = parseInt(stories);
      }
      
      const response = await axios.post(`${API_BASE_URL}/api/designs/generate`, requestData);

      // Get parameters from backend (already has overrides applied)
      const params = { ...response.data.houseParameters };
      
      // Include geometry from backend
      if (response.data.geometry) {
        params.geometry = response.data.geometry;
      }
      
      setHouseParams(params);
      setMeshData(response.data.mesh);
      setDesignInfo({
        styleName: response.data.styleName,
        designId: response.data.designId,
      });
      setErrorMessage('');
    } catch (error) {
      if (error.response?.data?.error) {
        setErrorMessage(error.response.data.error);
      } else if (error.message.includes('Network Error') || error.message.includes('ECONNREFUSED')) {
        setErrorMessage('Cannot connect to backend. Make sure the API is running at ' + API_BASE_URL);
      } else {
        setErrorMessage('Failed to generate design. Please try again.');
      }
      console.error('Error:', error);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <ScrollView 
      style={styles.scrollView} 
      contentContainerStyle={styles.scrollContent}
    >
      <View style={styles.header}>
          <Text style={styles.headerText}>🏗️ Architectural Dream Machine</Text>
          <Text style={styles.subHeaderText}>
            Generate your architectural design
          </Text>
        </View>

        <View style={styles.inputContainer}>
          <Text style={styles.label}>Lot Size (sq ft):</Text>
          <TextInput
            style={styles.input}
            value={lotSize}
            onChangeText={setLotSize}
            placeholder="e.g., 2500"
            keyboardType="numeric"
          />

          <Text style={styles.label}>Style Prompt:</Text>
          <TextInput
            style={styles.input}
            value={stylePrompt}
            onChangeText={setStylePrompt}
            placeholder="e.g., Modern minimalist, Victorian, Brutalist"
            multiline
          />

          <Text style={styles.label}>Building Layout:</Text>
          <View style={styles.pickerContainer}>
            <select 
              style={styles.picker}
              value={layoutType}
              onChange={(e) => setLayoutType(e.target.value)}
            >
              <option value="auto">🎲 Auto (Based on Lot Size)</option>
              <option value="cube">🟦 Traditional Cube</option>
              <option value="two-story">🏢 Two-Story (Compact Upper)</option>
              <option value="l-shape">🔲 L-Shaped</option>
              <option value="split-level">📐 Split-Level</option>
              <option value="angled">↗️ Angled Modern</option>
            </select>
          </View>

          <Text style={styles.label}>Number of Stories:</Text>
          <View style={styles.pickerContainer}>
            <select 
              style={styles.picker}
              value={stories}
              onChange={(e) => setStories(e.target.value)}
            >
              <option value="auto">🎲 Auto (Based on Style)</option>
              <option value="1">1️⃣ Single Story</option>
              <option value="2">2️⃣ Two Stories</option>
              <option value="3">3️⃣ Three Stories</option>
            </select>
          </View>

          <TouchableOpacity
            style={[styles.button, isLoading && styles.buttonDisabled]}
            onPress={handleGenerate}
            disabled={isLoading}
          >
            <Text style={styles.buttonText}>
              {isLoading ? 'Generating...' : 'Generate Design'}
            </Text>
          </TouchableOpacity>

          {errorMessage ? (
            <Text style={styles.errorText}>{errorMessage}</Text>
          ) : null}

          {designInfo && (
            <View style={styles.infoContainer}>
              <Text style={styles.infoText}>✅ Design Created Successfully!</Text>
              <Text style={styles.infoText}>Style: {designInfo.styleName}</Text>
              <Text style={styles.infoText}>Design ID: {designInfo.designId}</Text>
            </View>
          )}
        </View>

        {houseParams && (
          <View style={styles.resultContainer}>
            <Text style={styles.resultTitle}>Design Parameters</Text>
            
            {/* 3D Viewer - Web Only */}
            {Platform.OS === 'web' && meshData && (
              <View style={styles.viewer3DContainer}>
                <HouseViewer3D 
                  houseParams={houseParams}
                  mesh={meshData}
                />
                <TouchableOpacity
                  style={styles.downloadButton}
                  onPress={handleDownloadOBJ}
                >
                  <Text style={styles.downloadButtonText}>
                    📥 Download OBJ File
                  </Text>
                </TouchableOpacity>
              </View>
            )}

            <View style={styles.paramCard}>
              <Text style={styles.paramLabel}>🏠 Lot Size</Text>
              <Text style={styles.paramValue}>{houseParams.lotSize} sq ft</Text>
            </View>

            <View style={styles.paramCard}>
              <Text style={styles.paramLabel}>🏛️ Roof Type</Text>
              <Text style={styles.paramValue}>{houseParams.roofType}</Text>
            </View>

            <View style={styles.paramCard}>
              <Text style={styles.paramLabel}>🪟 Window Style</Text>
              <Text style={styles.paramValue}>{houseParams.windowStyle}</Text>
            </View>

            <View style={styles.paramCard}>
              <Text style={styles.paramLabel}>🚪 Room Count</Text>
              <Text style={styles.paramValue}>{houseParams.roomCount}</Text>
            </View>

            <View style={styles.paramCard}>
              <Text style={styles.paramLabel}>🎨 Material</Text>
              <Text style={styles.paramValue}>
                {houseParams.material.color} ({houseParams.material.texture})
              </Text>
            </View>

            <View style={styles.dimensionsCard}>
              <Text style={styles.paramLabel}>📐 Calculated Dimensions</Text>
              <Text style={styles.dimensionText}>
                Base: {Math.sqrt(houseParams.lotSize).toFixed(1)} ft × {Math.sqrt(houseParams.lotSize).toFixed(1)} ft
              </Text>
              <Text style={styles.dimensionText}>
                Height: {(Math.sqrt(houseParams.lotSize) * 0.6).toFixed(1)} ft
              </Text>
            </View>

            <Text style={styles.note}>
              💡 {Platform.OS === 'web' 
                ? 'Rotate the 3D model above to explore your design! Download the OBJ file to import into AutoCAD, Revit, or Blender.' 
                : '3D visualization is available in web browser mode. Run "npx expo start" and press "w" to view in browser.'}
            </Text>
          </View>
        )}
      </ScrollView>
  );
}

const styles = StyleSheet.create({
  scrollView: {
    backgroundColor: '#f5f5f5',
    ...(Platform.OS === 'web' && {
      flex: undefined, // Remove flex on web
      height: '100vh',
    })
  },
  scrollContent: {
    padding: 20,
  },
  header: {
    alignItems: 'center',
    marginBottom: 30,
    paddingTop: 10,
  },
  headerText: {
    fontSize: 26,
    fontWeight: 'bold',
    color: '#333',
    textAlign: 'center',
  },
  subHeaderText: {
    fontSize: 14,
    color: '#666',
    marginTop: 5,
  },
  inputContainer: {
    backgroundColor: '#fff',
    padding: 20,
    borderRadius: 10,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
    marginBottom: 20,
  },
  label: {
    fontSize: 16,
    fontWeight: '600',
    color: '#333',
    marginBottom: 8,
    marginTop: 10,
  },
  input: {
    borderWidth: 1,
    borderColor: '#ddd',
    borderRadius: 8,
    padding: 12,
    fontSize: 16,
    backgroundColor: '#fafafa',
  },
  pickerContainer: {
    borderWidth: 1,
    borderColor: '#ddd',
    borderRadius: 8,
    backgroundColor: '#fafafa',
    overflow: 'hidden',
  },
  picker: {
    width: '100%',
    padding: 12,
    fontSize: 16,
    border: 'none',
    backgroundColor: '#fafafa',
    cursor: 'pointer',
  },
  button: {
    backgroundColor: '#007AFF',
    padding: 16,
    borderRadius: 8,
    alignItems: 'center',
    marginTop: 20,
  },
  buttonDisabled: {
    backgroundColor: '#999',
  },
  buttonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
  errorText: {
    color: '#ff3b30',
    fontSize: 14,
    marginTop: 10,
    textAlign: 'center',
  },
  infoContainer: {
    marginTop: 15,
    padding: 15,
    backgroundColor: '#e8f5e9',
    borderRadius: 8,
    borderLeftWidth: 4,
    borderLeftColor: '#4caf50',
  },
  infoText: {
    fontSize: 14,
    color: '#2e7d32',
    marginVertical: 2,
    fontWeight: '500',
  },
  resultContainer: {
    backgroundColor: '#fff',
    borderRadius: 10,
    padding: 20,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  resultTitle: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 15,
    textAlign: 'center',
  },
  paramCard: {
    backgroundColor: '#f8f9fa',
    padding: 15,
    borderRadius: 8,
    marginBottom: 10,
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  paramLabel: {
    fontSize: 16,
    fontWeight: '600',
    color: '#555',
  },
  paramValue: {
    fontSize: 16,
    color: '#007AFF',
    fontWeight: '500',
  },
  dimensionsCard: {
    backgroundColor: '#fff3cd',
    padding: 15,
    borderRadius: 8,
    marginTop: 5,
    borderLeftWidth: 4,
    borderLeftColor: '#ffc107',
  },
  dimensionText: {
    fontSize: 14,
    color: '#856404',
    marginTop: 5,
  },
  viewer3DContainer: {
    marginBottom: 20,
    borderRadius: 10,
    overflow: 'hidden',
    borderWidth: 2,
    borderColor: '#007AFF',
  },
  downloadButton: {
    backgroundColor: '#4caf50',
    padding: 14,
    alignItems: 'center',
    marginTop: 10,
    marginHorizontal: 10,
    marginBottom: 10,
    borderRadius: 8,
  },
  downloadButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
  note: {
    fontSize: 13,
    color: '#666',
    marginTop: 20,
    padding: 15,
    backgroundColor: '#e3f2fd',
    borderRadius: 8,
    textAlign: 'center',
    lineHeight: 20,
  },
});
