# API Compatibility Layer - Quick Start Guide

## Setup

### 1. Configure Legacy JWT Settings

Add your legacy Node.js backend JWT key to `appsettings.json`:

```json
{
  "LegacyJwt": {
    "Key": "your-legacy-jwt-secret-key-here",
    "Issuer": "LanflixLegacy",
    "Audience": "LanflixLegacyClient"
  }
}
```

**Important**: The `Key` must match the JWT secret used in your Node.js backend.

### 2. Verify Middleware Registration

The compatibility layer is automatically registered in `Program.cs`:

```csharp
// API version detection
app.UseApiVersionDetection();

// Hybrid authentication (supports legacy tokens)
builder.Services.AddSingleton<ILegacyTokenService, LegacyTokenService>();

// Legacy response formatter
app.UseLegacyResponseFormatter();
```

## Testing

### Test Legacy Endpoints

```bash
# Test legacy content endpoint
curl http://localhost:5000/api/content?type=movie&page=1

# Expected: Returns content in legacy format
```

### Test Legacy Token

```bash
# Use a legacy token
curl -H "Authorization: Bearer YOUR_LEGACY_TOKEN" \
  http://localhost:5000/api/profiles

# Expected: Successfully authenticates
```

### Test Token Migration

```bash
# Migrate a legacy token
curl -X POST http://localhost:5000/api/auth/migrate-token \
  -H "Content-Type: application/json" \
  -d '{"legacyToken": "YOUR_LEGACY_TOKEN"}'

# Expected: Returns new token
{
  "success": true,
  "token": "NEW_JWT_TOKEN",
  "message": "Token successfully migrated to new format",
  "expiresIn": 43200
}
```

### Test Response Format

```bash
# Request legacy format explicitly
curl -H "X-Api-Format: legacy" \
  http://localhost:5000/api/library/items

# Expected: Response wrapped in legacy format
{
  "success": true,
  "data": { /* content */ },
  "message": "Success",
  "version": "2.0.0"
}
```

## Common Scenarios

### Scenario 1: Legacy Client (No Changes Required)

Legacy clients continue to work without any changes:

```javascript
// Legacy client code (unchanged)
fetch('http://localhost:5000/api/content?type=movie')
  .then(res => res.json())
  .then(data => {
    // data.success, data.data, data.message available
  });
```

### Scenario 2: New Client (Modern API)

New clients use the improved API:

```javascript
// New client code
fetch('http://localhost:5000/api/library/items?Type=Movie')
  .then(res => res.json())
  .then(data => {
    // Direct data access, no wrapper
  });
```

### Scenario 3: Gradual Migration

Client can migrate gradually:

```javascript
// Step 1: Migrate token on login
async function login(credentials) {
  const response = await fetch('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify(credentials)
  });
  
  const { token } = await response.json();
  
  // Check if it's a legacy token
  const validation = await fetch('/api/auth/validate-token', {
    method: 'POST',
    body: JSON.stringify({ token })
  });
  
  const { isLegacy, shouldMigrate } = await validation.json();
  
  if (shouldMigrate) {
    // Migrate to new format
    const migration = await fetch('/api/auth/migrate-token', {
      method: 'POST',
      body: JSON.stringify({ legacyToken: token })
    });
    
    const { token: newToken } = await migration.json();
    return newToken;
  }
  
  return token;
}

// Step 2: Update endpoints gradually
// Use new endpoints but request legacy format during transition
fetch('http://localhost:5000/api/library/items?Type=Movie', {
  headers: { 'X-Api-Format': 'legacy' }
});

// Step 3: Remove legacy format request
fetch('http://localhost:5000/api/library/items?Type=Movie');
```

## Monitoring

### Check Legacy Usage

Monitor logs for legacy client activity:

```bash
# Search logs for legacy API usage
grep "Legacy API:" logs/lanflix-*.log

# Search for legacy token validation
grep "legacy token" logs/lanflix-*.log
```

### Track Migration Progress

```bash
# Count legacy endpoint requests
grep "Legacy API:" logs/lanflix-*.log | wc -l

# Count token migrations
grep "Successfully migrated legacy token" logs/lanflix-*.log | wc -l
```

## Troubleshooting

### Issue: Legacy Token Not Working

**Symptoms**: 401 Unauthorized when using legacy token

**Solutions**:
1. Verify `LegacyJwt:Key` is set in `appsettings.json`
2. Ensure the key matches your Node.js backend
3. Check token hasn't expired
4. Verify issuer/audience match

```bash
# Validate token
curl -X POST http://localhost:5000/api/auth/validate-token \
  -H "Content-Type: application/json" \
  -d '{"token": "YOUR_TOKEN"}'
```

### Issue: Legacy Endpoint Not Found

**Symptoms**: 404 Not Found for legacy endpoints

**Solutions**:
1. Verify `LegacyApiController` is registered
2. Check endpoint path is correct
3. Review routing configuration

```bash
# Test specific endpoint
curl -v http://localhost:5000/api/content
```

### Issue: Response Not Wrapped

**Symptoms**: Response not in legacy format

**Solutions**:
1. Add `X-Api-Format: legacy` header
2. Or use `X-Api-Version: 1.0` header
3. Or use legacy endpoint path

```bash
# Force legacy format
curl -H "X-Api-Format: legacy" \
  http://localhost:5000/api/library/items
```

## Migration Checklist

- [ ] Configure `LegacyJwt:Key` in appsettings.json
- [ ] Test legacy endpoints work
- [ ] Test legacy tokens authenticate
- [ ] Test token migration endpoint
- [ ] Monitor legacy usage in logs
- [ ] Update client applications
- [ ] Test new endpoints
- [ ] Migrate user tokens
- [ ] Set deprecation timeline
- [ ] Remove compatibility layer

## API Reference

### Legacy Endpoints

| Method | Endpoint | Maps To |
|--------|----------|---------|
| GET | `/api/content` | `/api/library/items` |
| GET | `/api/content/:id` | `/api/library/items/:id` |
| POST | `/api/stream/start` | `/api/stream/:id/start` |
| GET | `/api/stream/:id` | `/api/stream/:sessionId/stream` |
| GET | `/api/profiles` | `/api/profiles` |
| GET | `/api/watchhistory/:profileId` | `/api/profiles/:id/history` |

### Migration Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/migrate-token` | Migrate legacy token |
| POST | `/api/auth/validate-token` | Validate token type |
| GET | `/api/auth/token-info` | Get current token info |

### Headers

| Header | Values | Description |
|--------|--------|-------------|
| `X-Api-Format` | `legacy` | Request legacy response format |
| `X-Api-Version` | `1.0`, `2.0` | Specify API version |
| `Authorization` | `Bearer <token>` | JWT token (legacy or new) |

## Support

For detailed documentation, see:
- `COMPATIBILITY_LAYER_README.md` - Complete usage guide
- `COMPATIBILITY_LAYER_IMPLEMENTATION.md` - Implementation details

For issues or questions, check the logs and diagnostics endpoints.
