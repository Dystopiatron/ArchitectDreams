import React from 'react';
import { Platform, View, Text, StyleSheet } from 'react-native';

/**
 * Cross-platform picker component that works on web and native platforms.
 * 
 * On web: Uses native HTML <select> element
 * On native: Uses @react-native-picker/picker if available, otherwise shows a placeholder
 * 
 * Props:
 * - value: Currently selected value
 * - onValueChange: Callback when value changes (receives new value)
 * - items: Array of { label: string, value: string } objects
 * - style: Optional style object for the picker container
 * - pickerStyle: Optional style object for the picker element itself
 */
export function CrossPlatformPicker({ 
  value, 
  onValueChange, 
  items, 
  style,
  pickerStyle 
}) {
  // Web platform - use native HTML select
  if (Platform.OS === 'web') {
    return (
      <View style={style}>
        <select 
          style={{
            width: '100%',
            padding: 10,
            fontSize: 16,
            borderRadius: 8,
            border: '1px solid #ccc',
            backgroundColor: '#fff',
            ...pickerStyle,
          }}
          value={value}
          onChange={(e) => onValueChange(e.target.value)}
        >
          {items.map(item => (
            <option key={item.value} value={item.value}>
              {item.label}
            </option>
          ))}
        </select>
      </View>
    );
  }

  // Native platforms - try to use @react-native-picker/picker
  try {
    // Dynamic require to avoid crash if package not installed
    const { Picker } = require('@react-native-picker/picker');
    
    return (
      <View style={style}>
        <Picker 
          selectedValue={value} 
          onValueChange={onValueChange}
          style={[styles.nativePicker, pickerStyle]}
        >
          {items.map(item => (
            <Picker.Item 
              key={item.value} 
              label={item.label} 
              value={item.value} 
            />
          ))}
        </Picker>
      </View>
    );
  } catch (error) {
    // Fallback if @react-native-picker/picker is not installed
    // Display the current selection as text
    const selectedItem = items.find(item => item.value === value);
    
    return (
      <View style={[styles.fallbackContainer, style]}>
        <Text style={styles.fallbackText}>
          {selectedItem?.label || 'Select an option'}
        </Text>
        <Text style={styles.fallbackHint}>
          (Install @react-native-picker/picker for native picker support)
        </Text>
      </View>
    );
  }
}

const styles = StyleSheet.create({
  nativePicker: {
    width: '100%',
    height: 50,
  },
  fallbackContainer: {
    padding: 12,
    backgroundColor: '#f0f0f0',
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#ccc',
  },
  fallbackText: {
    fontSize: 16,
    color: '#333',
  },
  fallbackHint: {
    fontSize: 12,
    color: '#888',
    marginTop: 4,
  },
});

export default CrossPlatformPicker;
