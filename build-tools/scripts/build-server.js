#!/usr/bin/env node

const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

/**
 * Build script for Lanflix server components
 * Builds both frontend and backend if they exist
 */

function runCommand(command, cwd = process.cwd()) {
    console.log(`Running: ${command}`);
    console.log(`In: ${cwd}`);
    
    try {
        execSync(command, { 
            cwd, 
            stdio: 'inherit',
            shell: true
        });
        return true;
    } catch (error) {
        console.error(`❌ Command failed: ${command}`);
        console.error(`Error: ${error.message}`);
        return false;
    }
}

function buildComponent(name, buildPath, buildCommand) {
    console.log(`\n📦 Building ${name}...`);
    console.log(`Path: ${buildPath}`);
    
    if (!fs.existsSync(buildPath)) {
        console.log(`⚠️  ${name} directory not found at ${buildPath}, skipping`);
        return true;
    }
    
    const packageJsonPath = path.join(buildPath, 'package.json');
    if (!fs.existsSync(packageJsonPath)) {
        console.log(`⚠️  No package.json found in ${buildPath}, skipping`);
        return true;
    }
    
    // Install dependencies if node_modules doesn't exist
    const nodeModulesPath = path.join(buildPath, 'node_modules');
    if (!fs.existsSync(nodeModulesPath)) {
        console.log(`Installing dependencies for ${name}...`);
        if (!runCommand('npm install', buildPath)) {
            return false;
        }
    }
    
    // Run build command
    console.log(`Building ${name}...`);
    return runCommand(buildCommand, buildPath);
}

function main() {
    console.log('🚀 Starting Lanflix server build...\n');
    
    const rootDir = process.cwd();
    let success = true;
    
    // Check for different possible server structures
    const serverPaths = [
        { name: 'Backend (Old)', path: 'lanflix-server/backend-old', command: 'npm run build' },
        { name: 'Frontend', path: 'server/frontend', command: 'npm run build' },
        { name: 'Backend', path: 'server/backend', command: 'npm run build' }
    ];
    
    for (const server of serverPaths) {
        const fullPath = path.join(rootDir, server.path);
        if (!buildComponent(server.name, fullPath, server.command)) {
            success = false;
            break;
        }
    }
    
    if (success) {
        console.log('\n✅ Server build completed successfully!');
    } else {
        console.log('\n❌ Server build failed!');
        process.exit(1);
    }
}

if (require.main === module) {
    main();
}