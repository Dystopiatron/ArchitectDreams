import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createStackNavigator } from '@react-navigation/stack';
import { Platform } from 'react-native';
import MainScreen from './screens/MainScreen';

const Stack = createStackNavigator();

// Fix scrolling on web
if (Platform.OS === 'web') {
  // Inject CSS to ensure body can scroll
  const style = document.createElement('style');
  style.textContent = `
    html, body, #root {
      height: 100%;
      overflow: auto !important;
    }
    body {
      overflow-y: scroll !important;
    }
  `;
  document.head.appendChild(style);
}

export default function App() {
  return (
    <NavigationContainer>
      <Stack.Navigator
        screenOptions={{
          headerShown: true,
          ...(Platform.OS === 'web' && {
            cardStyle: { flex: 1, overflow: 'auto' }
          })
        }}
      >
        <Stack.Screen 
          name="Main" 
          component={MainScreen}
          options={{ title: 'Architectural Dream Machine' }}
        />
      </Stack.Navigator>
    </NavigationContainer>
  );
}

