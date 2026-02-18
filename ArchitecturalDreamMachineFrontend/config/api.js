/**
 * API Configuration
 * 
 * This module centralizes all API-related configuration.
 * 
 * To configure for different environments:
 * 
 * 1. For local development (default):
 *    - Uses http://localhost:5095
 * 
 * 2. For physical device testing:
 *    - Set REACT_NATIVE_API_URL environment variable, or
 *    - Modify the API_BASE_URL below to your machine's IP
 * 
 * 3. For production:
 *    - Set REACT_NATIVE_API_URL to your production API URL
 */

// Get API URL from environment or use default
// For Expo, you can also configure this in app.json under "extra"
const getApiBaseUrl = () => {
  // Check for environment variable (works in Node.js dev environment)
  if (typeof process !== 'undefined' && process.env?.REACT_NATIVE_API_URL) {
    return process.env.REACT_NATIVE_API_URL;
  }
  
  // Default to localhost for development
  return 'http://localhost:5095';
};

const API_BASE_URL = getApiBaseUrl();

/**
 * API configuration object with all endpoints
 */
export const config = {
  /**
   * Base URL for the API
   */
  API_BASE_URL,
  
  /**
   * API endpoints
   */
  endpoints: {
    /**
     * Generate a new design
     * POST with { lotSize, stylePrompt, buildingShapeOverride?, storiesOverride? }
     */
    generate: `${API_BASE_URL}/api/designs/generate`,
    
    /**
     * Export a design as OBJ file
     * @param {number|string} designId - The design ID
     * @returns {string} Export endpoint URL
     */
    export: (designId) => `${API_BASE_URL}/api/designs/${designId}/export`,
    
    /**
     * Get all designs
     */
    getAll: `${API_BASE_URL}/api/designs`,
    
    /**
     * Get a specific design by ID
     * @param {number|string} designId - The design ID
     * @returns {string} Design endpoint URL
     */
    getById: (designId) => `${API_BASE_URL}/api/designs/${designId}`,
  },
};

export default config;
