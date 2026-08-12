import { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'com.transitpay.driver',
  appName: 'TransitPay Driver',
  webDir: 'dist',
  server: {
    androidScheme: 'https',
  },
  plugins: {
    StatusBar: {
      style: 'LIGHT',
      backgroundColor: '#1e40af',
    },
    Network: {},
    App: {},
  },
};

export default config;