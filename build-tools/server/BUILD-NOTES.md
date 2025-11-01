# Server Build Notes

## Executable Build Process

The server is now built as a standalone Windows executable using **pkg**.

### What's Different

**Before:**
- Copied all Node.js files and node_modules
- Required Node.js to be installed on target machine
- Large distribution size due to all dependencies

**After:**
- Single `lanflix-server.exe` file (~50-80MB)
- Includes Node.js runtime and all dependencies
- No Node.js installation required on target machine
- Much easier distribution

### Build Output

Running `build-installer.bat` creates:

1. **lanflix-server.exe** - Standalone executable
2. **lanflix-server-portable.zip** - Contains:
   - lanflix-server.exe
   - public/ folder (frontend assets)
   - .env configuration file
   - start-server.bat helper script
   - README.txt

### How It Works

The build process uses [pkg](https://github.com/vercel/pkg) to:
1. Bundle the compiled TypeScript (dist/ folder)
2. Include Node.js v18 runtime
3. Package all dependencies
4. Create a single executable

### Configuration

The pkg configuration in `server/backend/package.json`:
- **targets**: `node18-win-x64` (Windows 64-bit with Node 18)
- **assets**: Includes public files and .env.example
- **scripts**: Includes all compiled JavaScript from dist/

### Running the Server

Users can either:
- Double-click `lanflix-server.exe` directly
- Run `start-server.bat` (provides better console output)

### Troubleshooting

If the build fails:
1. Ensure TypeScript build completed (`npm run build:server`)
2. Check that pkg is installed globally (`npm install -g pkg`)
3. Verify Node.js version is 18 or higher
4. Check that dist/ folder exists in server/backend

### Future Improvements

- Add NSIS installer script for full installer experience
- Consider code signing for the executable
- Add auto-update functionality
- Create Linux and macOS builds
