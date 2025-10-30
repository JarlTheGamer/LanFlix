# Webhook Configuration Guide

Configure Sonarr and Radarr to automatically trigger library scans and conversions when downloads complete.

## Benefits

- ✅ **Instant Updates**: Library updates immediately when downloads finish
- ✅ **Auto-Conversion**: Files are converted to browser-compatible format right away
- ✅ **No Polling**: More efficient than checking every 60 seconds
- ✅ **Better UX**: Content appears in your library faster

---

## Radarr Webhook Setup

### 1. Open Radarr Settings

Navigate to: **Settings → Connect**

### 2. Add Webhook Connection

Click the **+** button and select **Webhook**

### 3. Configure Webhook

**Name**: `Lanflix`

**Triggers**: Check **On Download**

**URL**: `http://localhost:3000/api/webhook/radarr`

**Method**: `POST`

**Username**: (leave empty)

**Password**: (leave empty)

### 4. Test Connection

Click **Test** to verify it works, then **Save**

---

## Sonarr Webhook Setup

### 1. Open Sonarr Settings

Navigate to: **Settings → Connect**

### 2. Add Webhook Connection

Click the **+** button and select **Webhook**

### 3. Configure Webhook

**Name**: `Lanflix`

**Triggers**: Check **On Import Complete** (this is when the file is fully downloaded and ready)

**URL**: `http://localhost:3000/api/webhook/sonarr`

**Method**: `POST`

**Username**: (leave empty)

**Password**: (leave empty)

### 4. Test Connection

Click **Test** to verify it works, then **Save**

---

## How It Works

### Download Flow:

1. **User requests content** in Lanflix
2. **Sonarr/Radarr downloads** the file
3. **Download completes** → Webhook fires
4. **Lanflix receives notification**
5. **Library scan triggered** (finds new file)
6. **Auto-conversion starts** (if needed)
7. **File ready to play** in browser

### Webhook Payload:

Radarr sends:
```json
{
  "eventType": "Download",
  "movie": {
    "title": "Movie Title",
    "year": 2025
  },
  "movieFile": {
    "path": "/path/to/movie.mkv",
    "quality": {
      "quality": {
        "name": "Bluray-1080p"
      }
    }
  }
}
```

Sonarr sends:
```json
{
  "eventType": "Download",
  "series": {
    "title": "Series Title"
  },
  "episodes": [{
    "seasonNumber": 1,
    "episodeNumber": 1
  }],
  "episodeFile": {
    "path": "/path/to/episode.mkv",
    "quality": {
      "quality": {
        "name": "WEBDL-1080p"
      }
    }
  }
}
```

---

## Troubleshooting

### Webhook Not Firing

1. **Check Sonarr/Radarr logs** for webhook errors
2. **Verify URL** is correct (use your server IP if not localhost)
3. **Test endpoint** manually:
   ```bash
   curl http://localhost:3000/api/webhook/test
   ```

### Library Not Updating

1. **Check Lanflix logs** for webhook reception
2. **Verify file paths** match your media root configuration
3. **Check permissions** on media folders

### Conversion Not Starting

1. **Check logs** for conversion errors
2. **Verify FFmpeg** is installed and accessible
3. **Check disk space** for conversion output

---

## Advanced Configuration

### Remote Server Setup

If Lanflix is on a different machine than Sonarr/Radarr:

**Webhook URL**: `http://<lanflix-server-ip>:3000/api/webhook/radarr`

Example: `http://192.168.1.100:3000/api/webhook/radarr`

### Secure Webhooks (Optional)

Add authentication to webhook routes by modifying `backend/src/routes/webhook.routes.ts`:

```typescript
// Add middleware to verify a secret token
router.use((req, res, next) => {
  const token = req.headers['x-webhook-token'];
  if (token !== process.env.WEBHOOK_SECRET) {
    return res.status(401).json({ error: 'Unauthorized' });
  }
  next();
});
```

Then add to Sonarr/Radarr webhook config:
- **Custom Header**: `X-Webhook-Token: your-secret-token`

---

## Disabling Scheduled Scans

Once webhooks are configured, you can reduce the scheduled scan frequency:

Edit `backend/src/jobs/scheduler.ts`:

```typescript
// Change from every 6 hours to daily
this.scheduleJob('library-scan', '0 3 * * *', async () => {
  await this.scanLibrary();
});
```

Or disable completely by commenting out the job.

---

## Testing Webhooks

### Manual Test

Send a test webhook:

```bash
# Test Radarr webhook
curl -X POST http://localhost:3000/api/webhook/radarr \
  -H "Content-Type: application/json" \
  -d '{
    "eventType": "Download",
    "movie": {"title": "Test Movie"},
    "movieFile": {"path": "/path/to/test.mkv"}
  }'

# Test Sonarr webhook
curl -X POST http://localhost:3000/api/webhook/sonarr \
  -H "Content-Type: application/json" \
  -d '{
    "eventType": "Download",
    "series": {"title": "Test Series"},
    "episodes": [{"seasonNumber": 1, "episodeNumber": 1}],
    "episodeFile": {"path": "/path/to/test.mkv"}
  }'
```

### Check Logs

Watch Lanflix logs for webhook activity:

```bash
# Windows
Get-Content backend\logs\combined.log -Wait -Tail 50

# Linux/Mac
tail -f backend/logs/combined.log
```

Look for:
- `Radarr webhook received`
- `Sonarr webhook received`
- `Triggering library scan after download...`
- `Auto-converting movie/episode: ...`

---

## Summary

With webhooks configured:
- ✅ Downloads trigger immediate library updates
- ✅ Files are auto-converted to browser-compatible format
- ✅ Content appears in your library within seconds
- ✅ No more waiting for scheduled scans

**Next Steps**: Configure webhooks in Sonarr/Radarr and test with a download!
