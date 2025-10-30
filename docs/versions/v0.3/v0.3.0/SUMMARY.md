# Version 0.3.0 Summary

Quick overview of what changed in version 0.3.0.

## Release Information

- **Version**: 0.3.0
- **Release Date**: October 30, 2025
- **Codename**: "Documentation Dawn"
- **Type**: Minor Release
- **Breaking Changes**: None

## TL;DR

Version 0.3.0 is a **documentation-focused release** with video player improvements. No breaking changes, simple upgrade.

## What Changed

### 📚 Documentation (Major)
- Created comprehensive wiki with 9 organized folders
- Added 15+ documentation files (15,000+ words)
- Organized by topic: getting-started, architecture, api, features, tasks, troubleshooting, versions
- Added quick reference guide
- Complete API documentation with examples

### 🎬 Video Player (Bug Fix)
- Fixed audio initialization issues
- Better Content-Type detection for various formats
- Improved browser compatibility
- Enhanced unmuting logic

### 🔧 Backend (Enhancement)
- Better MIME type detection (MP4, MKV, WebM, AVI, MOV, M4V, TS)
- Improved streaming route
- Added Cache-Control headers

## Upgrade Instructions

```bash
# 1. Pull latest code
git pull origin main

# 2. Restart services (no migrations needed)
# Backend
cd backend && npm run dev

# Frontend (new terminal)
cd frontend && npm run dev
```

**That's it!** No database migrations, no configuration changes.

## Files Changed

### Documentation (New)
- `docs/README.md` - Wiki home
- `docs/QUICK-REFERENCE.md` - Quick reference
- `docs/WIKI-STRUCTURE.md` - Structure guide
- `docs/CHANGES.md` - Changes log
- `docs/getting-started/` - 2 files
- `docs/architecture/` - 1 file
- `docs/api/` - 1 file
- `docs/features/` - 1 file
- `docs/tasks/` - 3 files
- `docs/troubleshooting/` - 1 file
- `docs/versions/` - Version history

### Code (Modified)
- `backend/src/routes/streaming.routes.ts` - Enhanced streaming
- `frontend/src/modules/video-player.js` - Fixed audio

## Impact

### Users
- ✅ Better documentation
- ✅ Easier troubleshooting
- ✅ Video audio improvements
- ✅ No breaking changes

### Developers
- ✅ Clear architecture docs
- ✅ Complete API reference
- ✅ Contributing guidelines
- ✅ Development roadmap

## Known Issues

**Critical**:
- Video audio requires source files to have audio tracks (use ffprobe to check)

**Workaround**:
```bash
# Check audio
ffprobe -v error -select_streams a:0 -show_entries stream=codec_name video.mp4

# Re-encode if needed
ffmpeg -i input.mp4 -c:v copy -c:a aac -b:a 192k output.mp4
```

## What's Next

### v0.3.1 (Planned)
- Bug fixes
- Documentation improvements
- Performance tweaks

### v0.4.0 (Q4 2025)
- Continue watching row
- Search UI
- Download queue UI
- Settings page

## Quick Links

- [Full Changelog](./CHANGELOG.md)
- [Release Notes](./RELEASE-NOTES.md)
- [Migration Guide](./MIGRATION.md)
- [Documentation Home](../../../README.md)

---

**Questions?** Check the [documentation](../../../README.md) or [open an issue](https://github.com/yourusername/lanflix/issues).

---

**Last Updated**: October 30, 2025
