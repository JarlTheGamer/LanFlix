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
    rollupOptions: {
      input: {
        index: resolve(__dirname, 'src/index.html'),
        main: resolve(__dirname, 'src/pages/index.html'),
        settings: resolve(__dirname, 'src/pages/settings.html'),
        appConfig: resolve(__dirname, 'src/pages/app-config.html')
      }
    }
  },
  server: {
    port: 5173,
    open: '/pages/index.html',
    proxy: {
      '/api': {
        target: 'http://192.168.178.13:3000',
        changeOrigin: true
      }
    }
  }
});
