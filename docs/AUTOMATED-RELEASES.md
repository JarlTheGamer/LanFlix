# Automated Release System

Complete guide to using the automated release system that builds APKs and publishes them to GitHub automatically.

## Quick Start

```cmd
# One command to release everything!
npm run release
```

This will:
1. ✅ Bump version numbers
2. ✅ Build web assets
3. ✅ Build Android APK
4. ✅ Commit and tag in Git
5. ✅ Push to GitHub
6. ✅ Create GitHub release
7. ✅ Upload APK automatically

## Prerequisites

### Required Software

1. **Node.js** (v18+)
   ```cmd
   winget install OpenJS.NodeJS.LTS
   ```

2. **Java JDK** (17+)
   ```cmd
   winget install EclipseAdoptium.Temurin.17.JDK
   ```

3. **Git**
   ```cmd
   winget install Git.Git
   ```

4. **GitHub CLI** (for automatic uploads)
   ```cmd
   winget install GitHub.cli
   ```

### First-Time Setup

1. **Initialize Android project:**
   ```cmd
   cd frontend
   npm install
   npm run android:init
   ```

2. **Authenticate with GitHub:**
   ```cmd
   gh auth login
   ```
   Follow the prompts to authenticate.

3. **Verify setup:**
   ```cmd
   node --version
   java --version
   git --version
   gh --version
   ```

## Usage

### Option 1: Automated Release (Recommended)

```cmd
npm run release
```

You'll be prompted for:
- Version number (e.g., `1.0.1`) or bump type (`patch`, `minor`, `major`)

The script will handle everything else automatically!

### Option 2: Interactive Release (Custom Notes)

```cmd
npm run release:interactive
```

This allows you to write custom release notes during the process.

### Option 3: Manual Steps

If you prefer more control:

```cmd
# 1. Bump version
npm run version:bump 1.0.1

# 2. Build APK
cd frontend
npm run android:build-release

# 3. Commit and tag
git add .
git commit -m "Release v1.0.1"
git tag v1.0.1
git push origin main
git push origin v1.0.1

# 4. Create GitHub release
gh release create v1.0.1 releases/lanflix-android-v1.0.1.apk ^
    --title "Lanflix v1.0.1" ^
    --notes "Release notes here" ^
    --repo JarlTheGamer/Applications.
```

## Version Bumping

### Automatic Bumping

```cmd
npm run version:bump patch   # 1.0.0 -> 1.0.1
npm run version:bump minor   # 1.0.0 -> 1.1.0
npm run version:bump major   # 1.0.0 -> 2.0.0
```

### Specific Version

```cmd
npm run version:bump 1.2.3
```

### What Gets Updated

The version bump script updates:
- ✅ `frontend/package.json`
- ✅ `backend/package.json`
- ✅ `frontend/src/pages/index.html` (meta tag)
- ✅ `frontend/src/pages/settings.html` (meta tag)
- ✅ `frontend/src/modules/app-updater.js` (currentVersion)

## Release Process Details

### 1. Version Bump
- Updates all version numbers across the project
- Ensures consistency

### 2. Build Web Assets
- Runs `npm run build` in frontend
- Compiles and optimizes all web files
- Output: `frontend/dist/`

### 3. Sync to Capacitor
- Copies web assets to Android project
- Updates native configuration

### 4. Build APK
- Runs `gradlew assembleRelease`
- Creates optimized release APK
- Output: `frontend/build-tools/android/android/app/build/outputs/apk/release/app-release.apk`

### 5. Copy APK
- Renames APK with version number
- Copies to `releases/` folder
- Format: `lanflix-android-v1.0.1.apk`

### 6. Git Operations
- Commits all changes
- Creates version tag (e.g., `v1.0.1`)
- Pushes to GitHub

### 7. GitHub Release
- Creates release on GitHub
- Uploads APK as release asset
- Adds release notes
- Makes it available for in-app updates

## Release Notes

### Default Template

The automated script uses this template:

```markdown
## What's New in v1.0.1

### ✨ New Features
- Automated release system
- In-app update notifications

### 🐛 Bug Fixes
- Various bug fixes and improvements

### 🚀 Performance
- Improved app performance

## Installation
Download the APK and install on your Android device.

**Requirements:**
- Android 7.0 or higher
- Backend server running on your network
```

### Custom Release Notes

Use the interactive script:

```cmd
npm run release:interactive
```

Or edit the template in `scripts/release.bat`.

## Troubleshooting

### "GitHub CLI not found"

**Install GitHub CLI:**
```cmd
winget install GitHub.cli
```

**Or continue without it:**
The script will open your browser to manually create the release.

### "gh auth status failed"

**Authenticate:**
```cmd
gh auth login
```

Choose:
- GitHub.com
- HTTPS
- Login with a web browser

### "gradlew.bat not found"

**Initialize Android project:**
```cmd
cd frontend
npm run android:init
```

### "Failed to push tag"

**Check if tag already exists:**
```cmd
git tag -l
```

**Delete and recreate:**
```cmd
git tag -d v1.0.1
git push origin :refs/tags/v1.0.1
```

### "Release already exists"

**Delete the release on GitHub:**
1. Go to: https://github.com/JarlTheGamer/Applications./releases
2. Find the release
3. Click "Delete"

**Or use CLI:**
```cmd
gh release delete v1.0.1 --repo JarlTheGamer/Applications. --yes
```

### Build fails

**Clean and retry:**
```cmd
cd frontend/build-tools/android/android
gradlew.bat clean
cd ../../../..
npm run release
```

## Advanced Usage

### Pre-Release / Beta

For beta versions:

```cmd
# Bump to beta version
npm run version:bump 1.1.0-beta.1

# Build and release
npm run release

# Mark as pre-release on GitHub
gh release edit v1.1.0-beta.1 --prerelease --repo JarlTheGamer/Applications.
```

### Hotfix Release

For urgent fixes:

```cmd
# Create hotfix branch
git checkout -b hotfix/1.0.2

# Make your fixes
# ... edit files ...

# Release
npm run release

# Merge back
git checkout main
git merge hotfix/1.0.2
git push origin main
```

### Rollback a Release

If you need to undo a release:

```cmd
# Delete GitHub release
gh release delete v1.0.1 --repo JarlTheGamer/Applications. --yes

# Delete tag
git tag -d v1.0.1
git push origin :refs/tags/v1.0.1

# Revert commit
git revert HEAD
git push origin main
```

## CI/CD Integration (Future)

### GitHub Actions

Create `.github/workflows/release.yml`:

```yaml
name: Release

on:
  push:
    tags:
      - 'v*'

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
        with:
          node-version: 18
      - uses: actions/setup-java@v3
        with:
          distribution: 'temurin'
          java-version: '17'
      
      - name: Build APK
        run: |
          cd frontend
          npm install
          npm run build
          npx cap sync android
          cd build-tools/android/android
          ./gradlew assembleRelease
      
      - name: Create Release
        uses: softprops/action-gh-release@v1
        with:
          files: frontend/build-tools/android/android/app/build/outputs/apk/release/app-release.apk
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

Then just push a tag to trigger:
```cmd
git tag v1.0.1
git push origin v1.0.1
```

## Best Practices

### Before Releasing

- [ ] Test the app thoroughly
- [ ] Update documentation
- [ ] Review all changes
- [ ] Check version number is correct
- [ ] Ensure all tests pass

### Release Checklist

- [ ] Version bumped
- [ ] APK built successfully
- [ ] APK tested on device
- [ ] Git committed and tagged
- [ ] Pushed to GitHub
- [ ] GitHub release created
- [ ] APK uploaded
- [ ] Release notes written
- [ ] In-app update tested

### After Releasing

- [ ] Verify release on GitHub
- [ ] Test in-app update
- [ ] Announce on social media
- [ ] Update documentation
- [ ] Monitor for issues

## Monitoring Releases

### Check Release Status

```cmd
gh release list --repo JarlTheGamer/Applications.
```

### View Release Details

```cmd
gh release view v1.0.1 --repo JarlTheGamer/Applications.
```

### Download Statistics

```cmd
gh release view v1.0.1 --json assets --repo JarlTheGamer/Applications.
```

## Security

### Signing APKs

For production releases, sign your APKs:

1. Generate keystore (one time):
   ```cmd
   keytool -genkey -v -keystore lanflix-release.keystore ^
       -alias lanflix -keyalg RSA -keysize 2048 -validity 10000
   ```

2. Configure in `frontend/build-tools/android/android/app/build.gradle`

3. Store keystore securely (don't commit to git!)

### GitHub Token

For CI/CD, use a GitHub token:

```cmd
# Create token at: https://github.com/settings/tokens
# Add to repository secrets as GITHUB_TOKEN
```

## Support

### Getting Help

1. Check this documentation
2. Review error messages
3. Check GitHub Actions logs (if using CI/CD)
4. Open an issue on GitHub

### Common Issues

- **Build fails**: Clean gradle cache
- **Upload fails**: Check GitHub authentication
- **Version conflict**: Delete existing tag/release
- **APK not found**: Check build output path

## Related Documentation

- [BUILD-WITHOUT-STUDIO.md](../frontend/build-tools/android/BUILD-WITHOUT-STUDIO.md)
- [IN-APP-UPDATES.md](./IN-APP-UPDATES.md)
- [RELEASE-GUIDE.md](./RELEASE-GUIDE.md)

## Quick Reference

```cmd
# Full automated release
npm run release

# Interactive with custom notes
npm run release:interactive

# Just bump version
npm run version:bump 1.0.1

# Just build APK
cd frontend && npm run android:build-release

# Manual GitHub release
gh release create v1.0.1 releases/lanflix-android-v1.0.1.apk ^
    --title "Lanflix v1.0.1" ^
    --notes "Release notes" ^
    --repo JarlTheGamer/Applications.
```
