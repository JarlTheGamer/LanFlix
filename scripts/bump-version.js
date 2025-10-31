#!/usr/bin/env node

/**
 * Version Bump Script
 * Automatically updates version numbers across all files
 * 
 * Usage:
 *   node scripts/bump-version.js 1.0.1
 *   node scripts/bump-version.js patch  (1.0.0 -> 1.0.1)
 *   node scripts/bump-version.js minor  (1.0.0 -> 1.1.0)
 *   node scripts/bump-version.js major  (1.0.0 -> 2.0.0)
 */

const fs = require('fs');
const path = require('path');

// Files to update
const FILES_TO_UPDATE = [
  'frontend/package.json',
  'backend/package.json',
  'frontend/src/pages/index.html',
  'frontend/src/pages/settings.html',
  'frontend/src/modules/app-updater.js'
];

// Get current version from frontend package.json
function getCurrentVersion() {
  const packagePath = path.join(__dirname, '../frontend/package.json');
  const packageJson = JSON.parse(fs.readFileSync(packagePath, 'utf8'));
  return packageJson.version || '1.0.0';
}

// Parse version string
function parseVersion(version) {
  const parts = version.split('.').map(Number);
  return {
    major: parts[0] || 0,
    minor: parts[1] || 0,
    patch: parts[2] || 0
  };
}

// Calculate new version
function calculateNewVersion(current, bump) {
  const version = parseVersion(current);

  switch (bump.toLowerCase()) {
    case 'major':
      version.major++;
      version.minor = 0;
      version.patch = 0;
      break;
    case 'minor':
      version.minor++;
      version.patch = 0;
      break;
    case 'patch':
      version.patch++;
      break;
    default:
      // Assume it's a specific version number
      return bump;
  }

  return `${version.major}.${version.minor}.${version.patch}`;
}

// Update package.json files
function updatePackageJson(filePath, newVersion) {
  const fullPath = path.join(__dirname, '..', filePath);
  const content = fs.readFileSync(fullPath, 'utf8');
  const packageJson = JSON.parse(content);
  
  packageJson.version = newVersion;
  
  fs.writeFileSync(fullPath, JSON.stringify(packageJson, null, 2) + '\n', 'utf8');
  console.log(`✅ Updated ${filePath}`);
}

// Update HTML files
function updateHtmlFile(filePath, newVersion) {
  const fullPath = path.join(__dirname, '..', filePath);
  let content = fs.readFileSync(fullPath, 'utf8');
  
  // Update meta tag
  content = content.replace(
    /<meta name="app-version" content="[^"]*" \/>/,
    `<meta name="app-version" content="${newVersion}" />`
  );
  
  fs.writeFileSync(fullPath, content, 'utf8');
  console.log(`✅ Updated ${filePath}`);
}

// Update app-updater.js
function updateAppUpdater(filePath, newVersion) {
  const fullPath = path.join(__dirname, '..', filePath);
  let content = fs.readFileSync(fullPath, 'utf8');
  
  // Update currentVersion
  content = content.replace(
    /this\.currentVersion = '[^']*';/,
    `this.currentVersion = '${newVersion}';`
  );
  
  fs.writeFileSync(fullPath, content, 'utf8');
  console.log(`✅ Updated ${filePath}`);
}

// Main function
function main() {
  const args = process.argv.slice(2);
  
  if (args.length === 0) {
    console.error('❌ Error: Please specify a version or bump type');
    console.log('\nUsage:');
    console.log('  node scripts/bump-version.js 1.0.1');
    console.log('  node scripts/bump-version.js patch');
    console.log('  node scripts/bump-version.js minor');
    console.log('  node scripts/bump-version.js major');
    process.exit(1);
  }

  const bump = args[0];
  const currentVersion = getCurrentVersion();
  const newVersion = calculateNewVersion(currentVersion, bump);

  console.log(`\n🔄 Bumping version: ${currentVersion} → ${newVersion}\n`);

  // Update all files
  try {
    updatePackageJson('frontend/package.json', newVersion);
    updatePackageJson('backend/package.json', newVersion);
    updateHtmlFile('frontend/src/pages/index.html', newVersion);
    updateHtmlFile('frontend/src/pages/settings.html', newVersion);
    updateAppUpdater('frontend/src/modules/app-updater.js', newVersion);

    console.log(`\n✨ Successfully bumped version to ${newVersion}!`);
    console.log('\nNext steps:');
    console.log('  1. Review the changes: git diff');
    console.log('  2. Commit the changes: git add . && git commit -m "Bump version to ' + newVersion + '"');
    console.log('  3. Create a tag: git tag v' + newVersion);
    console.log('  4. Push: git push && git push --tags');
    console.log('  5. Create a GitHub release with the tag v' + newVersion);
  } catch (error) {
    console.error('❌ Error updating files:', error.message);
    process.exit(1);
  }
}

main();
