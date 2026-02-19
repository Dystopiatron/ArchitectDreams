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
import { CrossPlatformPicker } from '../components/CrossPlatformPicker';
import { config } from '../config/api';

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
        config.endpoints.export(designInfo.designId),
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
      
      const response = await axios.post(config.endpoints.generate, requestData);

      // Validate response structure before using
      const data = response.data;
      if (!data || !data.houseParameters || typeof data.houseParameters !== 'object') {
        setErrorMessage('Invalid response from server');
        return;
      }
      if (typeof data.designId !== 'number' || !data.styleName) {
        setErrorMessage('Invalid response from server');
        return;
      }

      // Get parameters from backend (already has overrides applied)
      const params = { ...data.houseParameters };

      // Include geometry from backend
      if (data.geometry) {
        params.geometry = data.geometry;
      }

      setHouseParams(params);
      setMeshData(data.mesh);
      setDesignInfo({
        styleName: data.styleName,
        designId: data.designId,
      });
      setErrorMessage('');
    } catch (error) {
      if (error.response?.data?.error) {
        setErrorMessage(error.response.data.error);
      } else if (error.message.includes('Network Error') || error.message.includes('ECONNREFUSED')) {
        setErrorMessage('Cannot connect to backend. Make sure the API is running at ' + config.API_BASE_URL);
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
            maxLength={500}
          />

          <Text style={styles.label}>Building Layout:</Text>
          <View style={styles.pickerContainer}>
            <CrossPlatformPicker
              value={layoutType}
              onValueChange={setLayoutType}
              pickerStyle={styles.picker}
              items={[
                { value: 'auto', label: '🎲 Auto (Based on Style)' },
                { value: 'cube', label: '🟦 Traditional Cube' },
                { value: 'l-shape', label: '🔲 L-Shaped' },
                { value: 'split-level', label: '📐 Split-Level' },
                { value: 'angled', label: '↗️ Angled Modern' },
              ]}
            />
          </View>

          <Text style={styles.label}>Number of Stories:</Text>
          <View style={styles.pickerContainer}>
            <CrossPlatformPicker
              value={stories}
              onValueChange={setStories}
              pickerStyle={styles.picker}
              items={[
                { value: 'auto', label: '🎲 Auto (Based on Style)' },
                { value: '1', label: '1️⃣ Single Story' },
                { value: '2', label: '2️⃣ Two Stories' },
                { value: '3', label: '3️⃣ Three Stories' },
              ]}
            />
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
    backgroundColor: '#1a1a1a', // Dark background
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
    color: '#e0e0e0', // Light text
    textAlign: 'center',
  },
  subHeaderText: {
    fontSize: 14,
    color: '#b0b0b0', // Muted light text
    marginTop: 5,
  },
  inputContainer: {
    backgroundColor: '#2a2a2a', // Dark card
    padding: 20,
    borderRadius: 10,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.3,
    shadowRadius: 4,
    elevation: 3,
    marginBottom: 20,
  },
  label: {
    fontSize: 16,
    fontWeight: '600',
    color: '#e0e0e0', // Light text
    marginBottom: 8,
    marginTop: 10,
  },
  input: {
    borderWidth: 1,
    borderColor: '#444',
    borderRadius: 8,
    padding: 12,
    fontSize: 16,
    backgroundColor: '#333', // Dark input
    color: '#e0e0e0', // Light text
  },
  pickerContainer: {
    borderWidth: 1,
    borderColor: '#444',
    borderRadius: 8,
    backgroundColor: '#333', // Dark picker
    overflow: 'hidden',
  },
  picker: {
    width: '100%',
    padding: 12,
    fontSize: 16,
    border: 'none',
    backgroundColor: '#333', // Dark picker
    color: '#e0e0e0', // Light text
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
    backgroundColor: '#2a3a2a', // Dark green-ish
    borderRadius: 8,
    borderLeftWidth: 4,
    borderLeftColor: '#66bb6a', // Lighter green accent
  },
  infoText: {
    fontSize: 14,
    color: '#a5d6a7', // Light green text
    marginVertical: 2,
    fontWeight: '500',
  },
  resultContainer: {
    backgroundColor: '#2a2a2a', // Dark card
    borderRadius: 10,
    padding: 20,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.3,
    shadowRadius: 4,
    elevation: 3,
  },
  resultTitle: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#e0e0e0', // Light text
    marginBottom: 15,
    textAlign: 'center',
  },
  paramCard: {
    backgroundColor: '#333', // Dark card background
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
    color: '#b0b0b0', // Light gray text
  },
  paramValue: {
    fontSize: 16,
    color: '#64b5f6', // Softer blue
    fontWeight: '500',
  },
  dimensionsCard: {
    backgroundColor: '#3a3a2a', // Dark yellow-ish
    padding: 15,
    borderRadius: 8,
    marginTop: 5,
    borderLeftWidth: 4,
    borderLeftColor: '#ffb300', // Amber accent
  },
  dimensionText: {
    fontSize: 14,
    color: '#ffd54f', // Light amber text
    marginTop: 5,
  },
  viewer3DContainer: {
    marginBottom: 20,
    borderRadius: 10,
    overflow: 'hidden',
    borderWidth: 2,
    borderColor: '#64b5f6', // Softer blue border
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
    color: '#90caf9', // Light blue text
    marginTop: 20,
    padding: 15,
    backgroundColor: '#2a2a3a', // Dark blue-ish
    borderRadius: 8,
    textAlign: 'center',
    lineHeight: 20,
  },
});
