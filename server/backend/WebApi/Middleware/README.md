# Middleware and Cross-Cutting Concerns

This document describes the middleware and cross-cutting concerns implemented for the Lanflix backend.

## Overview

The middleware layer provides global exception handling, authentication, authorization, rate limiting, and CORS configuration for the application.

## Components

### 1. Exception Handling Middleware

**File**: `ExceptionHandlingMiddleware.cs`

Provides centralized exception handling for the entire application.

**Features**:
- Catches and handles `NotFoundException` (404 responses)
- Catches and handles `ValidationException` (400 responses with validation errors)
- Catches and handles `TranscodingException` (500 responses with FFmpeg output)
- Catches all unhandled exceptions (500 responses)
- Structured logging for all exceptions
- Development-only stack trace exposure

**Usage**:
```csharp
app.UseMiddleware<ExceptionHandlingMiddleware>();
```

### 2. Authentication & Authorization

**Files**:
- `Infrastructure/Services/Authentication/TokenService.cs` - JWT token generation and validation
- `Application/Common/Interfaces/ITokenService.cs` - Token service interface
- `WebApi/Authorization/ProfileAuthorizationHandler.cs` - Profile-based authorization
- `WebApi/Controllers/AuthController.cs` - Authentication endpoints

**Features**:
- JWT-based authentication
- Profile-based authorization (users can only access their own data)
- Admin role support
- Token validation endpoint
- SignalR authentication support (via query string token)

**Configuration** (appsettings.json):
```json
{
  "Jwt": {
    "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "Lanflix",
    "Audience": "LanflixClient",
    "ExpirationMinutes": 43200
  }
}
```

**Endpoints**:
- `POST /api/auth/login` - Authenticate with profile ID and receive JWT token
- `GET /api/auth/validate` - Validate current token

**Authorization Policies**:
- `AdminOnly` - Requires Admin role
- `ProfileOwner` - Requires user to be the owner of the profile being accessed

**Usage in Controllers**:
```csharp
[Authorize] // Requires authentication
[Authorize(Policy = "AdminOnly")] // Requires admin role
[Authorize(Policy = "ProfileOwner")] // Requires profile ownership
```

### 3. Rate Limiting

**Configuration**: Configured in `Program.cs`

**Policies**:

1. **Global Rate Limiter**
   - 100 requests per minute per IP address
   - Applied to all endpoints by default

2. **Streaming Policy** (`streaming`)
   - Max 3 concurrent streams per authenticated user or IP
   - Applied to streaming endpoints
   - Usage: `[EnableRateLimiting("streaming")]`

3. **Per-User Policy** (`per-user`)
   - 200 requests per minute per authenticated user or IP
   - For general API calls
   - Usage: `[EnableRateLimiting("per-user")]`

4. **Strict Policy** (`strict`)
   - 10 requests per minute per IP
   - For sensitive operations (login, admin operations)
   - Usage: `[EnableRateLimiting("strict")]`

**Response**:
When rate limit is exceeded, returns 429 (Too Many Requests) with:
```json
{
  "statusCode": 429,
  "message": "Too many requests. Please try again later.",
  "retryAfter": 60
}
```

### 4. CORS Configuration

**Configuration**: Configured in `Program.cs` and `appsettings.json`

**Policies**:

1. **Default Policy**
   - Allows configured origins from appsettings.json
   - Allows all methods and headers
   - Allows credentials (required for SignalR and JWT)
   - Exposes custom headers (Content-Disposition, X-Pagination)

2. **Production Policy**
   - Stricter configuration for production environments
   - Only allows specific methods (GET, POST, PUT, DELETE, PATCH)
   - Only allows specific headers
   - Configurable via `Lanflix:Cors:ProductionOrigins`
   - Usage: `[EnableCors("Production")]`

3. **Public Policy**
   - Allows any origin, method, and header
   - No credentials support
   - For public endpoints only
   - Usage: `[EnableCors("Public")]`

**Configuration** (appsettings.json):
```json
{
  "Lanflix": {
    "Cors": {
      "AllowedOrigins": [
        "http://localhost:3000",
        "http://localhost:5173",
        "http://localhost:8080",
        "http://localhost:4200"
      ],
      "ProductionOrigins": [
        "https://yourdomain.com"
      ]
    }
  }
}
```

## Middleware Pipeline Order

The middleware pipeline is configured in the following order (order matters!):

1. **ExceptionHandlingMiddleware** - Must be first to catch all exceptions
2. **HttpsRedirection** - Redirect HTTP to HTTPS
3. **CORS** - Handle cross-origin requests
4. **RateLimiter** - Apply rate limiting
5. **OutputCache** - Cache responses
6. **Authentication** - Authenticate requests
7. **Authorization** - Authorize requests
8. **Endpoints** - Route to controllers and hubs

## Security Considerations

### JWT Token Security
- Tokens expire after 30 days (configurable)
- Tokens are signed with HMAC-SHA256
- Secret key must be at least 32 characters
- Tokens include profile ID and name claims
- Admin role is included in token claims

### Rate Limiting
- Prevents brute force attacks on login endpoint
- Prevents abuse of streaming resources
- Prevents API abuse
- Per-user limits for authenticated users
- Per-IP limits for anonymous users

### CORS
- Restricts which origins can access the API
- Credentials support for authenticated requests
- Configurable for different environments
- Preflight caching for performance

### Input Validation
- All exceptions are logged with structured logging
- Validation errors return detailed error information
- Stack traces only exposed in development mode
- Sensitive information redacted from logs

## Testing

### Testing Authentication
```bash
# Login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"profileId": 1}'

# Validate token
curl -X GET http://localhost:5000/api/auth/validate \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Testing Rate Limiting
```bash
# Exceed rate limit
for i in {1..150}; do
  curl http://localhost:5000/api/library/items
done
```

### Testing CORS
```bash
# Preflight request
curl -X OPTIONS http://localhost:5000/api/library/items \
  -H "Origin: http://localhost:3000" \
  -H "Access-Control-Request-Method: GET"
```

## Requirements Satisfied

- ✅ **Requirement 9.1**: Structured logging for all exceptions
- ✅ **Requirement 9.2**: Appropriate HTTP status codes for errors
- ✅ **Requirement 9.3**: Detailed error information in development mode
- ✅ **Requirement 9.4**: Error logging with stack traces
- ✅ **Requirement 9.5**: Sensitive data redaction
- ✅ **Requirement 2.6**: JWT authentication and authorization
- ✅ **Requirement 13.7**: Rate limiting for API endpoints
- ✅ **Requirement 2.1**: CORS configuration for client applications

## Future Enhancements

1. **API Key Authentication** - For third-party integrations
2. **OAuth2/OpenID Connect** - For social login
3. **Two-Factor Authentication** - For enhanced security
4. **IP Whitelisting** - For admin endpoints
5. **Request Throttling** - Based on endpoint complexity
6. **Distributed Rate Limiting** - Using Redis for multi-server deployments
