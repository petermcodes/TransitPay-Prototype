import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import path from 'path';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(import.meta.dirname, './src'),
    },
  },
  build: {
    outDir: 'dist',
    assetsDir: 'assets',
    sourcemap: false,
  },
  server: {
    host: '0.0.0.0',
    allowedHosts: [
      'localhost',
      '127.0.0.1',
      '192.168.50.28',
      'five-unit-feminine.ngrok-free.dev',
      '.ngrok-free.dev',
    ],
    proxy: {
      '/api': {
        target: 'http://127.0.0.1:5132',
        changeOrigin: true,
      },
    },
  },
});