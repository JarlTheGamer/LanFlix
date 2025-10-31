# Lanflix Scripts

Automation scripts for building and releasing Lanflix.

## Available Scripts

### `release.bat`
**Fully automated release process**

Builds APK and publishes to GitHub automatically.

```cmd
npm run release
```

Features:
- ✅ Automatic version bumping
- ✅ Web asset building
- ✅ APK compilation
- ✅ Git commit and tagging
- ✅ GitHub release creation
- ✅ APK upload

Works with or without GitHub CLI installed.

---

### `release-interactive.bat`
**Interactive release with custom notes**

Same as `release.bat` but prompts for custom release notes.

```cmd
npm run release:interactive
```

---

### `bump-version.js`
**Version number updater**

Updates version across all project files.

```cmd
npm run version:bump 1.0.1
npm run version:bump patch
npm run version:bump minor
npm run version:bump major
```

Updates:
- `frontend/package.json`
- `backend/package.json`
- `frontend/src/pages/index.html`
- `frontend/src/pages/settings.html`
- `frontend/src/modules/app-updater.js`

---

## Quick Start

### First Time Setup

1. **Install prerequisites:**
   ```cmd
   winget install OpenJS.NodeJS.LTS
   winget install EclipseAdoptium.Temurin.17.JDK
   winget install Git.Git
   winget install GitHub.cli
   ```

2. **Authenticate with GitHub:**
   ```cmd
   gh auth login
   ```

3. **Initialize Android project:**
   ```cmd
   cd frontend
   npm install
   npm run android:init
   ```

### Release a New Version

```cmd
npm run release
```

Enter version when prompted (e.g., `1.0.1` or `patch`).

That's it! The script handles everything else.

## Documentation

- [AUTOMATED-RELEASES.md](../docs/AUTOMATED-RELEASES.md) - Complete guide
- [BUILD-WITHOUT-STUDIO.md](../frontend/build-tools/android/BUILD-WITHOUT-STUDIO.md) - Building APKs
- [RELEASE-GUIDE.md](../docs/RELEASE-GUIDE.md) - Manual release process

## Troubleshooting

### GitHub CLI not installed

Install it:
```cmd
winget install GitHub.cli
```

Or the script will open your browser for manual upload.

### gradlew not found

Initialize Android project:
```cmd
cd frontend
npm run android:init
```

### Authentication failed

Login to GitHub:
```cmd
gh auth login
```

## Support

For issues, check the [AUTOMATED-RELEASES.md](../docs/AUTOMATED-RELEASES.md) troubleshooting section.
