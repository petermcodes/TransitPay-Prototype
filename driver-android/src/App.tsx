import { Capacitor } from '@capacitor/core';
import { StatusBar, Style } from '@capacitor/status-bar';
import { Network } from '@capacitor/network';
import DriverApp from './DriverApp';

// Initialize Capacitor plugins
async function initializeCapacitor() {
  if (Capacitor.isNativePlatform()) {
    try {
      // Set status bar style
      await StatusBar.setStyle({ style: Style.Light });
      await StatusBar.setBackgroundColor({ color: '#1e40af' });
    } catch (error) {
      console.error('Error initializing Capacitor:', error);
    }
  }
}

// Initialize on app start
initializeCapacitor().catch(console.error);

export default function App() {
  return <DriverApp />;
}