const axios = require('axios');

// Trigger library scan via internal service
async function triggerScan() {
  try {
    // Import and run the library service directly
    const path = require('path');
    const tsNode = require('ts-node');
    
    // Register TypeScript
    tsNode.register({
      project: path.join(__dirname, 'tsconfig.json'),
      transpileOnly: true
    });

    // Import the library service
    const { libraryService } = require('./src/services');
    
    console.log('Starting library scan...');
    const result = await libraryService.scanLibraryFolder();
    console.log('Library scan completed:', result);
    process.exit(0);
  } catch (error) {
    console.error('Error:', error);
    process.exit(1);
  }
}

triggerScan();
