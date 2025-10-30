/**
 * Test script to verify FFmpeg probe functionality
 * Usage: node test-ffmpeg.js <path-to-video-file>
 */

const { probeMedia, needsTranscoding } = require('./dist/utils/ffmpeg');

async function testFFmpeg(filePath) {
  console.log('Testing FFmpeg probe on:', filePath);
  console.log('='.repeat(60));

  try {
    // Probe media file
    console.log('\n📊 Media Information:');
    const info = await probeMedia(filePath);
    console.log(JSON.stringify(info, null, 2));

    // Check transcoding needs
    console.log('\n🔄 Transcoding Analysis:');
    const transcodeCheck = await needsTranscoding(filePath);
    console.log(JSON.stringify(transcodeCheck, null, 2));

    // Summary
    console.log('\n📝 Summary:');
    if (transcodeCheck.needsTranscode) {
      console.log('❌ This file needs transcoding');
      if (transcodeCheck.transcodeAudio) {
        console.log('   → Audio will be transcoded to AAC');
      }
      if (transcodeCheck.transcodeVideo) {
        console.log('   → Video will be transcoded to H.264');
      }
      if (!transcodeCheck.transcodeVideo && transcodeCheck.transcodeAudio) {
        console.log('   → Video will be copied (fast!)');
      }
    } else {
      console.log('✅ This file can be direct played (no transcoding needed)');
    }

    console.log('\n' + '='.repeat(60));
  } catch (error) {
    console.error('❌ Error:', error.message);
    process.exit(1);
  }
}

// Get file path from command line
const filePath = process.argv[2];

if (!filePath) {
  console.error('Usage: node test-ffmpeg.js <path-to-video-file>');
  process.exit(1);
}

testFFmpeg(filePath);
