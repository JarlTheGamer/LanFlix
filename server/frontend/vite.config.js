import { defineConfig } from 'vite';
import { resolve } from 'path';
import { fileURLToPath } from 'url';
import { dirname } from 'path';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

export default defineConfig({
  root: resolve(__dirname, 'src'),
  publicDir: resolve(__dirname, 'src/public'),
  build: {
    outDir: resolve(__dirname, 'dist'),
    emptyOutDir: true,
    target: 'esnext',
    rollupOptions: {
      input: {
        index: resolve(__dirname, 'src/index.html'),
        main: resolve(__dirname, 'src/pages/index.html'),
        profiles: resolve(__dirname, 'src/pages/profiles.html'),
        settings: resolve(__dirname, 'src/pages/settings.html'),
        appConfig: resolve(__dirname, 'src/pages/app-config.html'),
        myList: resolve(__dirname, 'src/pages/my-list.html'),
        notifications: resolve(__dirname, 'src/pages/notifications.html'),
        player: resolve(__dirname, 'src/pages/player.html'),
        admin: resolve(__dirname, 'src/pages/admin.html')
      }
    }
  },
  server: {
    port: 5173,
    open: '/pages/index.html',
    proxy: {
      '/api': {
        target: 'http://localhost:6129',
        changeOrigin: true
      },
      '/health': {
        target: 'http://localhost:6129',
        changeOrigin: true
      },
      '/media': {
        target: 'http://localhost:6129',
        changeOrigin: true
      },
      '/images': {
        target: 'http://localhost:6129',
        changeOrigin: true
      }
    }
  }
});
