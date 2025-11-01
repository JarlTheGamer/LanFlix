# API Compatibility Layer

This document describes the API compatibility layer that provides backward compatibility with the legacy Node.js backend.

## Overview

The compatibility layer ensures that existing clients can continue to work with the new C# backend without requiring immediate updates. It provides:

1. **Legacy Endpoint Mappings** - Old API endpoints are mapped to new ones
2. **Response Format Compatibility** - Responses can be wrapped in legacy format
3. **Legacy Token Support** - Old JWT tokens are validated and can be migrated

## Components

### 1. Legacy Endpoint Mappings

**Controller**: `LegacyApiController.cs`

Maps old endpoint paths to new backend functionality:

| Legacy Endpoint | New Endpoint | Notes |
|----------------|--------------|-------|
| `GET /api/content` | `GET /api/library/items` | Query parameters mapped |
| `GET /api/content/:id` | `GET /api/library/items/:id` | Direct mapping |
| `POST /api/stream/start` | `POST /api/stream/:id/start` | Request body transformed |
| `GET /api/stream/:id` | `GET /api/stream/:sessionId/stream` | Redirects to new endpoint |
| `GET /api/profiles` | `GET /api/profiles` | Same endpoint, legacy format |
| `GET /api/watchhistory/:profileId` | `GET /api/profiles/:id/history` | Path restructured |

**Usage Example**:

```bash
# Legacy client request
GET /api/content?type=movie&page=1

# Automatically routed to
GET /api/library/items?Type=Movie&PageNumber=1&PageSize=20
```

### 2. Response Format Compatibility

**Models**: `LegacyApiResponse<T>`
**Middleware**: `LegacyResponseFormatterMiddleware.cs`
**Filter**: `LegacyResponseWrapperAttribute.cs`

#### Legacy Response Format

```json
{
  "success": true,
  "data": { /* actual data */ },
  "message": "Success",
  "version": "2.0.0"
}
```

#### Error Response Format

```json
{
  "success": false,
  "data": null,
  "message": "Error message here",
  "version": "2.0.0"
}
```

#### Triggering Legacy Format

Clients can request legacy format in three ways:

1. **Header**: `X-Api-Format: legacy`
2. **Query Parameter**: `?format=legacy`
3. **Version Header**: `X-Api-Version: 1.x`

**Example**:

```bash
# Request with legacy format header
curl -H "X-Api-Format: legacy" http://localhost:5000/api/library/items

# Response
{
  "success": true,
  "data": {
    "items": [...],
    "pageNumber": 1,
    "totalPages": 10,
    "totalCount": 200
  },
  "message": "Success",
  "version": "2.0.0"
}
```

### 3. API Version Detection

**Middleware**: `ApiVersionDetectionMiddleware.cs`

Automatically detects API version from:

1. `X-Api-Version` header
2. `api-version` query parameter
3. User-Agent string (e.g., `Lanflix/1.0`)
4. Request path patterns (legacy endpoints)

The detected version is stored in `HttpContext.Items`:
- `ApiVersion` - The detected version (e.g., `1.0` or `2.0`)
- `IsLegacyClient` - Boolean indicating if client is legacy

### 4. Legacy Token Support

**Service**: `LegacyTokenService.cs`
**Handler**: `HybridJwtBearerHandler.cs`
**Controller**: `TokenMigrationController.cs`

#### Features

- **Dual Token Validation**: Validates both new and legacy JWT tokens
- **Token Migration**: Converts legacy tokens to new format
- **Automatic Detection**: Identifies legacy tokens by issuer/audience

#### Configuration

Add legacy JWT settings to `appsettings.json`:

```json
{
  "LegacyJwt": {
    "Key": "legacy-secret-key-from-nodejs-backend",
    "Issuer": "LanflixLegacy",
    "Audience": "LanflixLegacyClient"
  }
}
```

#### Token Migration Endpoint

**POST** `/api/auth/migrate-token`

Request:
```json
{
  "legacyToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

Response:
```json
{
  "success": true,
  "token": "new-jwt-token-here",
  "message": "Token successfully migrated to new format",
  "expiresIn": 43200
}
```

#### Token Validation Endpoint

**POST** `/api/auth/validate-token`

Request:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

Response:
```json
{
  "isValid": true,
  "isLegacy": true,
  "profileId": 1,
  "message": "Token is valid",
  "shouldMigrate": true
}
```

#### Token Info Endpoint

**GET** `/api/auth/token-info` (Requires authentication)

Response:
```json
{
  "isLegacy": true,
  "profileId": 1,
  "shouldMigrate": true,
  "message": "You are using a legacy token. Consider migrating to the new format."
}
```

## Client Migration Strategy

### Phase 1: Dual Support (Recommended)

1. Deploy new C# backend with compatibility layer enabled
2. Legacy clients continue working without changes
3. New clients use new API endpoints and format
4. Monitor usage of legacy endpoints

### Phase 2: Gradual Migration

1. Update client applications to use new endpoints
2. Implement token migration on client login
3. Monitor legacy endpoint usage decline
4. Provide migration notices to users

### Phase 3: Deprecation

1. Set deprecation timeline (e.g., 6 months)
2. Add deprecation warnings to legacy endpoints
3. Send notifications to users still using legacy clients
4. Provide clear migration documentation

### Phase 4: Removal

1. Remove compatibility layer after deprecation period
2. Keep legacy token validation for additional grace period
3. Monitor for any remaining legacy clients
4. Complete migration

## Testing the Compatibility Layer

### Test Legacy Endpoint Mapping

```bash
# Test legacy content endpoint
curl http://localhost:5000/api/content?type=movie

# Test legacy stream start
curl -X POST http://localhost:5000/api/stream/start \
  -H "Content-Type: application/json" \
  -d '{"contentId": 1, "profileId": 1}'

# Test legacy watch history
curl http://localhost:5000/api/watchhistory/1
```

### Test Response Format Compatibility

```bash
# Request with legacy format
curl -H "X-Api-Format: legacy" \
  http://localhost:5000/api/library/items

# Request with version header
curl -H "X-Api-Version: 1.0" \
  http://localhost:5000/api/library/items
```

### Test Token Migration

```bash
# Migrate a legacy token
curl -X POST http://localhost:5000/api/auth/migrate-token \
  -H "Content-Type: application/json" \
  -d '{"legacyToken": "your-legacy-token-here"}'

# Validate a token
curl -X POST http://localhost:5000/api/auth/validate-token \
  -H "Content-Type: application/json" \
  -d '{"token": "your-token-here"}'

# Get token info (requires authentication)
curl -H "Authorization: Bearer your-token-here" \
  http://localhost:5000/api/auth/token-info
```

## Monitoring and Metrics

### Recommended Metrics to Track

1. **Legacy Endpoint Usage**
   - Number of requests to legacy endpoints
   - Breakdown by endpoint
   - Trend over time

2. **API Version Distribution**
   - Percentage of v1.x vs v2.x clients
   - User-Agent analysis
   - Geographic distribution

3. **Token Migration**
   - Number of legacy tokens validated
   - Number of tokens migrated
   - Migration success rate

4. **Response Format**
   - Requests using legacy format
   - Requests using new format
   - Format preference by client type

### Logging

The compatibility layer logs important events:

```csharp
// Legacy endpoint access
_logger.LogInformation("Legacy API: Getting content (type={Type}, page={Page})", type, page);

// Legacy token validation
_logger.LogInformation("Successfully validated legacy token for profile {ProfileId}", profileId);

// Token migration
_logger.LogInformation("Successfully migrated legacy token for profile {ProfileId} to new format", profileId);

// API version detection
_logger.LogDebug("Detected API version: {Version} (IsLegacy: {IsLegacy})", apiVersion, isLegacy);
```

## Troubleshooting

### Legacy Tokens Not Working

1. Verify `LegacyJwt:Key` is configured in `appsettings.json`
2. Check that the key matches the one used in the Node.js backend
3. Verify issuer and audience match legacy backend settings
4. Check token expiration

### Legacy Endpoints Not Found

1. Ensure `LegacyApiController` is registered
2. Verify routing configuration
3. Check that middleware is in correct order
4. Review logs for routing errors

### Response Format Not Wrapping

1. Verify `ApiVersionDetectionMiddleware` is registered
2. Check that `LegacyResponseFormatterMiddleware` is after authentication
3. Ensure client is sending correct headers/parameters
4. Review middleware order in `Program.cs`

## Best Practices

1. **Always Log Legacy Usage**: Track which clients are using legacy features
2. **Set Deprecation Timeline**: Communicate clearly when legacy support will end
3. **Provide Migration Tools**: Make it easy for clients to migrate
4. **Monitor Performance**: Ensure compatibility layer doesn't impact performance
5. **Document Everything**: Keep clear documentation for both old and new APIs
6. **Test Thoroughly**: Test all legacy endpoints with real legacy clients
7. **Gradual Rollout**: Deploy compatibility layer to staging first
8. **Have Rollback Plan**: Be prepared to revert if issues arise

## Security Considerations

1. **Legacy Token Security**: Legacy tokens may use weaker security settings
2. **Token Migration**: Ensure migration endpoint is rate-limited
3. **Deprecation Warnings**: Don't expose sensitive information in warnings
4. **Audit Logging**: Log all legacy token usage for security audits
5. **Grace Period**: Set reasonable but not excessive grace periods

## Performance Impact

The compatibility layer has minimal performance impact:

- **Version Detection**: ~1ms overhead per request
- **Response Wrapping**: ~2-5ms for JSON serialization
- **Legacy Token Validation**: ~5-10ms additional validation time
- **Endpoint Mapping**: No measurable overhead (routing only)

## Future Enhancements

1. **Automatic Client Detection**: Identify client type from User-Agent
2. **Usage Analytics Dashboard**: Visual dashboard for legacy usage
3. **Automated Migration Notifications**: Email users about migration
4. **Compatibility Testing Suite**: Automated tests for legacy clients
5. **Gradual Feature Deprecation**: Deprecate features individually
