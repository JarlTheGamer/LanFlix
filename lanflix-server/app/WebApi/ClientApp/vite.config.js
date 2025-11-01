import { defineConfig } from 'vite';
import { resolve } from 'path';
import { fileURLToPath } from 'url';
import { dirname } from 'path';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

export default defineConfig({
  root: resolve(__dirname),
  publicDir: resolve(__dirname, 'public'),
  build: {
    outDir: resolve(__dirname, '../wwwroot'),
    emptyOutDir: true,
    target: 'esnext',
    rollupOptions: {
      input: {
        index: resolve(__dirname, 'index.html'),
        main: resolve(__dirname, 'pages/index.html'),
        profiles: resolve(__dirname, 'pages/profiles.html'),
        settings: resolve(__dirname, 'pages/settings.html'),
        appConfig: resolve(__dirname, 'pages/app-config.html'),
        myList: resolve(__dirname, 'pages/my-list.html'),
        notifications: resolve(__dirname, 'pages/notifications.html'),
        player: resolve(__dirname, 'pages/player.html'),
        admin: resolve(__dirname, 'pages/admin.html')
      }
    }
  },
  server: {
    port: 5173,
    open: '/pages/index.html',
    proxy: {
      '/api': {
        target: 'http://localhost:5037',
        changeOrigin: true
      },
      '/health': {
        target: 'http://localhost:5037',
        changeOrigin: true
      },
      '/media': {
        target: 'http://localhost:5037',
        changeOrigin: true
      },
      '/images': {
        target: 'http://localhost:5037',
        changeOrigin: true
      }
    }
  }
});
