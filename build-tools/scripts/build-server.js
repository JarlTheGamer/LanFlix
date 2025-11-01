/**
 * Build script for Lanflix server
 * Builds frontend and copies to backend for serving
 */

const fs = require('fs-extra');
const path = require('path');
const { execSync } = require('child_process');

const ROOT_DIR = path.join(__dirname, '../..');
const FRONTEND_DIR = path.join(ROOT_DIR, 'server/frontend');
const BACKEND_DIR = path.join(ROOT_DIR, 'server/backend');
const FRONTEND_DIST = path.join(FRONTEND_DIR, 'dist');
const BACKEND_PUBLIC = path.join(BACKEND_DIR, 'public');

console.log('🚀 Building Lanflix Server...\n');

// Step 1: Build frontend
console.log('📦 Building frontend...');
try {
  execSync('npm run build', {
    cwd: FRONTEND_DIR,
    stdio: 'inherit'
  });
  console.log('✅ Frontend built successfully\n');
} catch (error) {
  console.error('❌ Frontend build failed');
  process.exit(1);
}

// Step 2: Copy frontend dist to backend public folder
console.log('📁 Copying frontend to backend...');
try {
  // Remove old public folder
  if (fs.existsSync(BACKEND_PUBLIC)) {
    fs.removeSync(BACKEND_PUBLIC);
  }

  // Copy dist to public
  fs.copySync(FRONTEND_DIST, BACKEND_PUBLIC);
  console.log('✅ Frontend copied to backend\n');
} catch (error) {
  console.error('❌ Failed to copy frontend:', error.message);
  process.exit(1);
}

// Step 3: Build backend
console.log('🔨 Building backend...');
try {
  execSync('npm run build', {
    cwd: BACKEND_DIR,
    stdio: 'inherit'
  });
  console.log('✅ Backend built successfully\n');
} catch (error) {
  console.error('❌ Backend build failed');
  process.exit(1);
}

console.log('🎉 Server build complete!');
console.log('\nTo run the server:');
console.log('  cd server/backend');
console.log('  npm start');
