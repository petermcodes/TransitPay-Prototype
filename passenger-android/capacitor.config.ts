import { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'com.transitpay.passenger',
  appName: 'TransitPay Passenger',
  webDir: 'dist',
  server: {
    androidScheme: 'https',
  },
  plugins: {
    StatusBar: {
      style: 'LIGHT',
      backgroundColor: '#1e40af',
    },
    SecureStorage: {},
    Network: {},
    App: {},
  },
};

export default config;