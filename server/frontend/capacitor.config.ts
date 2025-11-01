import { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'com.lanflix.app',
  appName: 'Lanflix',
  webDir: 'dist',
  android: {
    path: '../build-tools/android/android',
    allowMixedContent: true,
    captureInput: true,
    webContentsDebuggingEnabled: true
  },
  server: {
    androidScheme: 'http',
    hostname: 'localhost',
    // Allow cleartext traffic for local backend connection
    cleartext: true
  }
};

export default config;
