#!/usr/bin/env node

const fs = require('fs');
const path = require('path');

/**
 * Bump version script for Lanflix
 * Updates version in package.json, Android build.gradle, and backend package.json
 */

function parseVersion(version) {
    const parts = version.split('.').map(Number);
    if (parts.length !== 3 || parts.some(isNaN)) {
        throw new Error(`Invalid version format: ${version}`);
    }
    return parts;
}

function incrementVersion(version, bumpType) {
    const [major, minor, patch] = parseVersion(version);
    
    switch (bumpType) {
        case 'major':
            return `${major + 1}.0.0`;
        case 'minor':
            return `${major}.${minor + 1}.0`;
        case 'patch':
            return `${major}.${minor}.${patch + 1}`;
        default:
            // Assume it's a specific version
            parseVersion(bumpType); // Validate format
            return bumpType;
    }
}

function updatePackageJson(filePath, newVersion) {
    if (!fs.existsSync(filePath)) {
        console.log(`⚠️  ${filePath} not found, skipping`);
        return;
    }
    
    const content = fs.readFileSync(filePath, 'utf8');
    const packageJson = JSON.parse(content);
    const oldVersion = packageJson.version;
    
    packageJson.version = newVersion;
    
    fs.writeFileSync(filePath, JSON.stringify(packageJson, null, 2) + '\n');
    console.log(`✓ Updated ${filePath}: ${oldVersion} → ${newVersion}`);
}

function updateAndroidBuildGradle(filePath, newVersion) {
    if (!fs.existsSync(filePath)) {
        console.log(`⚠️  ${filePath} not found, skipping`);
        return;
    }
    
    let content = fs.readFileSync(filePath, 'utf8');
    
    // Update versionName
    const versionNameRegex = /versionName\s+"([^"]+)"/;
    const oldVersionMatch = content.match(versionNameRegex);
    const oldVersion = oldVersionMatch ? oldVersionMatch[1] : 'unknown';
    
    content = content.replace(versionNameRegex, `versionName "${newVersion}"`);
    
    // Update versionCode (increment by 1)
    const versionCodeRegex = /versionCode\s+(\d+)/;
    const versionCodeMatch = content.match(versionCodeRegex);
    if (versionCodeMatch) {
        const oldCode = parseInt(versionCodeMatch[1]);
        const newCode = oldCode + 1;
        content = content.replace(versionCodeRegex, `versionCode ${newCode}`);
        console.log(`✓ Updated ${filePath}: ${oldVersion} (code ${oldCode}) → ${newVersion} (code ${newCode})`);
    } else {
        console.log(`✓ Updated ${filePath}: ${oldVersion} → ${newVersion}`);
    }
    
    fs.writeFileSync(filePath, content);
}

function main() {
    const args = process.argv.slice(2);
    
    if (args.length !== 1) {
        console.error('Usage: node bump-version.js <version|patch|minor|major>');
        console.error('Examples:');
        console.error('  node bump-version.js 1.2.3');
        console.error('  node bump-version.js patch');
        console.error('  node bump-version.js minor');
        console.error('  node bump-version.js major');
        process.exit(1);
    }
    
    const versionInput = args[0];
    
    try {
        // Get current version from main package.json
        const mainPackagePath = path.join(process.cwd(), 'package.json');
        const mainPackage = JSON.parse(fs.readFileSync(mainPackagePath, 'utf8'));
        const currentVersion = mainPackage.version;
        
        // Calculate new version
        const newVersion = incrementVersion(currentVersion, versionInput);
        
        console.log(`Bumping version: ${currentVersion} → ${newVersion}`);
        console.log('');
        
        // Update all version files
        updatePackageJson(mainPackagePath, newVersion);
        updatePackageJson(path.join(process.cwd(), 'lanflix-server/backend-old/package.json'), newVersion);
        updateAndroidBuildGradle(path.join(process.cwd(), 'build-tools/android/app/build.gradle'), newVersion);
        
        console.log('');
        console.log(`✅ Version bump complete: ${newVersion}`);
        
    } catch (error) {
        console.error('❌ Error:', error.message);
        process.exit(1);
    }
}

if (require.main === module) {
    main();
}

module.exports = { incrementVersion, parseVersion };