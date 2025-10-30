# Migration Guide - v0.2.x to v0.3.0

Guide for upgrading from version 0.2.x to 0.3.0.

## Overview

Version 0.3.0 is a **non-breaking release** focused on documentation improvements and bug fixes. No code changes are required for existing installations.

## Breaking Changes

✅ **None** - This release has no breaking changes.

## What Changed

### Documentation Structure

The documentation has been completely reorganized:

**Old Structure**:
```
docs/
└── Task.md  (single file)
```

**New Structure**:
```
docs/
├── README.md
├── getting-started/
├── architecture/
├── api/
├── features/
├── tasks/
├── troubleshooting/
└── versions/
```

### Code Changes

**Backend**:
- Enhanced `backend/src/routes/streaming.routes.ts` with better Content-Type detection
- No API changes
- No database schema changes

**Frontend**:
- Improved `frontend/src/modules/video-player.js` audio initialization
- No interface changes
- No breaking changes

## Migration Steps

### 1. Backup (Recommended)

```bash
# Backup database
cp backend/data/lanflix.db backend/data/lanflix.db.backup

# Backup configuration
cp backend/.env backend/.env.backup
```

### 2. Update Code

```bash
# Pull latest changes
git pull origin main

# Or download latest release
# wget https://github.com/yourusername/lanflix/archive/v0.3.0.zip
```

### 3. Update Dependencies

```bash
# Backend (no changes, but good practice)
cd backend
npm install

# Frontend (no changes, but good practice)
cd ../frontend
npm install
```

### 4. Restart Services

```bash
# Stop existing services
# Ctrl+C in terminal or:
pkill -f "node.*lanflix"

# Start backend
cd backend
npm run dev

# Start frontend (new terminal)
cd frontend
npm run dev
```

### 5. Verify Installation

```bash
# Check backend health
curl http://localhost:3000/health

# Expected response:
# {"status":"ok","timestamp":"2025-10-30T..."}

# Check API status
curl http://localhost:3000/api/settings/api-status

# Test video streaming
curl -I http://localhost:3000/api/stream/123
```

## Configuration Changes

### No Changes Required

All existing configuration remains valid:
- `.env` file unchanged
- Database schema unchanged
- API endpoints unchanged
- Frontend routes unchanged

### Optional: Update Documentation Bookmarks

If you had bookmarks to old documentation:
- `docs/Task.md` → See new structure in `docs/README.md`
- Update any internal documentation links

## Database Migration

### No Migration Required

Version 0.3.0 requires **no database migrations**.

Your existing database will work without changes:
```bash
# No need to run migrations
# npm run migrate  # NOT NEEDED
```

## API Changes

### No Breaking Changes

All existing API endpoints remain unchanged:
- ✅ `/api/content/*` - No changes
- ✅ `/api/library/*` - No changes
- ✅ `/api/stream/*` - No changes (enhanced internally)
- ✅ `/api/profiles/*` - No changes
- ✅ `/api/settings/*` - No changes

### Enhanced Endpoints

**`GET /api/stream/:id`**
- Now properly detects Content-Type for various video formats
- Better range request handling
- No client-side changes needed

## Frontend Changes

### No Breaking Changes

All existing frontend code remains compatible:
- ✅ Video player API unchanged
- ✅ Module interfaces unchanged
- ✅ State management unchanged
- ✅ Routing unchanged

### Enhanced Features

**Video Player**
- Better audio initialization (automatic)
- Improved browser compatibility (automatic)
- No code changes needed in your usage

## Troubleshooting

### Issue: Videos Still Have No Audio

**Cause**: Source video files may lack audio tracks.

**Solution**:
```bash
# Check if video has audio
ffprobe -v error -select_streams a:0 -show_entries stream=codec_name -of default=noprint_wrappers=1:nokey=1 video.mp4

# If no output, re-encode with audio
ffmpeg -i input.mp4 -c:v copy -c:a aac -b:a 192k output.mp4
```

See [Video Playback Troubleshooting](../../troubleshooting/video-playback.md) for details.

### Issue: Documentation Links Broken

**Cause**: Documentation structure changed.

**Solution**:
- Start at [docs/README.md](../../README.md)
- Use navigation links to find content
- Check [WIKI-STRUCTURE.md](../../WIKI-STRUCTURE.md) for complete structure

### Issue: Port Already in Use

**Cause**: Old process still running.

**Solution**:
```bash
# Find and kill old process
# Windows
netstat -ano | findstr :3000
taskkill /PID <PID> /F

# Mac/Linux
lsof -ti:3000 | xargs kill -9
```

## Rollback Procedure

If you need to rollback to v0.2.x:

```bash
# 1. Stop services
pkill -f "node.*lanflix"

# 2. Checkout previous version
git checkout v0.2.x

# 3. Restore database (if backed up)
cp backend/data/lanflix.db.backup backend/data/lanflix.db

# 4. Restore configuration (if backed up)
cp backend/.env.backup backend/.env

# 5. Reinstall dependencies
cd backend && npm install
cd ../frontend && npm install

# 6. Restart services
cd backend && npm run dev
# In new terminal:
cd frontend && npm run dev
```

## Post-Migration Checklist

- [ ] Backend starts without errors
- [ ] Frontend loads correctly
- [ ] Can browse content
- [ ] Can play videos
- [ ] Audio works (if source files have audio)
- [ ] Profiles load correctly
- [ ] Watch history preserved
- [ ] External services connected (Sonarr, Radarr, etc.)

## Getting Help

If you encounter issues during migration:

1. Check [Known Issues](../../tasks/known-issues.md)
2. Review [Troubleshooting Guide](../../troubleshooting/video-playback.md)
3. Check backend logs: `backend/logs/error.log`
4. Search [GitHub Issues](https://github.com/yourusername/lanflix/issues)
5. Ask in [Discussions](https://github.com/yourusername/lanflix/discussions)

## What's Next

After migrating, check out:
- [Quick Reference](../../QUICK-REFERENCE.md) - Common tasks
- [API Overview](../../api/overview.md) - API documentation
- [Feature Roadmap](../../tasks/roadmap.md) - Upcoming features

## Summary

✅ **No breaking changes**  
✅ **No database migration needed**  
✅ **No configuration changes required**  
✅ **Simple git pull and restart**  
✅ **All existing functionality preserved**  

This is a **safe upgrade** focused on documentation and minor improvements.

---

**Migration Difficulty**: ⭐☆☆☆☆ (Very Easy)  
**Estimated Time**: 5 minutes  
**Downtime Required**: ~1 minute (restart services)

---

**Last Updated**: October 30, 2025
