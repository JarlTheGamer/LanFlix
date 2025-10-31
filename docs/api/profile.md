# Profile API

API endpoints for user profile management and watchlists.

## Base URL

```
/api/profiles
```

## Endpoints

### List Profiles

Get all user profiles.

```http
GET /api/profiles
```

**Response:**
```json
{
  "count": 3,
  "profiles": [
    {
      "id": 1,
      "name": "John",
      "avatarColorPrimary": "#FF5733",
      "avatarColorSecondary": "#C70039",
      "createdAt": "2025-10-01T10:00:00.000Z"
    },
    {
      "id": 2,
      "name": "Jane",
      "avatarColorPrimary": "#3498DB",
      "avatarColorSecondary": "#2874A6",
      "createdAt": "2025-10-02T11:00:00.000Z"
    }
  ]
}
```

**Example:**
```javascript
const response = await fetch('/api/profiles');
const { profiles } = await response.json();
```

### Create Profile

Create a new user profile.

```http
POST /api/profiles
```

**Request Body:**
```json
{
  "name": "John",
  "avatarColorPrimary": "#FF5733",
  "avatarColorSecondary": "#C70039"
}
```

**Response:**
```json
{
  "message": "Profile created successfully",
  "profile": {
    "id": 1,
    "name": "John",
    "avatarColorPrimary": "#FF5733",
    "avatarColorSecondary": "#C70039",
    "createdAt": "2025-10-31T12:00:00.000Z"
  }
}
```

**Example:**
```javascript
const response = await fetch('/api/profiles', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    name: 'John',
    avatarColorPrimary: '#FF5733',
    avatarColorSecondary: '#C70039'
  })
});
```

### Get Profile

Get specific profile details.

```http
GET /api/profiles/:id
```

**Path Parameters:**
- `id` (required) - Profile ID

**Response:**
```json
{
  "id": 1,
  "name": "John",
  "avatarColorPrimary": "#FF5733",
  "avatarColorSecondary": "#C70039",
  "createdAt": "2025-10-01T10:00:00.000Z"
}
```

**Example:**
```javascript
const response = await fetch('/api/profiles/1');
```

### Update Profile

Update profile information.

```http
PUT /api/profiles/:id
```

**Path Parameters:**
- `id` (required) - Profile ID

**Request Body:**
```json
{
  "name": "Johnny",
  "avatarColorPrimary": "#E74C3C",
  "avatarColorSecondary": "#C0392B"
}
```

**Response:**
```json
{
  "message": "Profile updated successfully",
  "profile": {
    "id": 1,
    "name": "Johnny",
    "avatarColorPrimary": "#E74C3C",
    "avatarColorSecondary": "#C0392B",
    "createdAt": "2025-10-01T10:00:00.000Z"
  }
}
```

**Example:**
```javascript
const response = await fetch('/api/profiles/1', {
  method: 'PUT',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    name: 'Johnny'
  })
});
```

### Delete Profile

Delete a profile.

```http
DELETE /api/profiles/:id
```

**Path Parameters:**
- `id` (required) - Profile ID

**Response:**
```json
{
  "message": "Profile deleted successfully",
  "id": 1
}
```

**Example:**
```javascript
const response = await fetch('/api/profiles/1', {
  method: 'DELETE'
});
```

### Get Watchlist

Get profile's My List (watchlist).

```http
GET /api/profiles/:id/watchlist
```

**Path Parameters:**
- `id` (required) - Profile ID

**Response:**
```json
{
  "profileId": 1,
  "count": 5,
  "items": [
    {
      "id": 1,
      "addedAt": "2025-10-30T15:00:00.000Z",
      "content": {
        "id": 123,
        "tmdbId": 550,
        "type": "movie",
        "title": "Fight Club",
        "overview": "An insomniac office worker...",
        "posterPath": "https://...",
        "voteAverage": 8.4,
        "releaseDate": "1999-10-15"
      }
    }
  ]
}
```

**Example:**
```javascript
const response = await fetch('/api/profiles/1/watchlist');
const { items } = await response.json();
```

### Add to Watchlist

Add content to profile's My List.

```http
POST /api/profiles/:id/watchlist/:contentId
```

**Path Parameters:**
- `id` (required) - Profile ID
- `contentId` (required) - Content ID

**Response:**
```json
{
  "message": "Content added to watchlist",
  "watchlistItem": {
    "id": 1,
    "profileId": 1,
    "contentId": 123,
    "addedAt": "2025-10-31T12:00:00.000Z"
  }
}
```

**Example:**
```javascript
const response = await fetch('/api/profiles/1/watchlist/123', {
  method: 'POST'
});
```

### Remove from Watchlist

Remove content from profile's My List.

```http
DELETE /api/profiles/:id/watchlist/:contentId
```

**Path Parameters:**
- `id` (required) - Profile ID
- `contentId` (required) - Content ID

**Response:**
```json
{
  "message": "Content removed from watchlist",
  "profileId": 1,
  "contentId": 123
}
```

**Example:**
```javascript
const response = await fetch('/api/profiles/1/watchlist/123', {
  method: 'DELETE'
});
```

## Validation

### Avatar Colors

Avatar colors must be valid hex colors:
- Format: `#RRGGBB`
- Example: `#FF5733`

Invalid colors will return:
```json
{
  "error": "Avatar colors must be valid hex colors (e.g., #FF5733)",
  "code": "VALIDATION_ERROR"
}
```

### Profile Name

- Required field
- String type
- No length restrictions

## Error Responses

### 400 Bad Request
```json
{
  "error": "avatarColorPrimary must be a valid hex color",
  "code": "VALIDATION_ERROR"
}
```

### 404 Not Found
```json
{
  "error": "Profile not found",
  "code": "NOT_FOUND"
}
```

## Use Cases

### Profile Selection Flow

```javascript
// 1. Get all profiles
const { profiles } = await fetch('/api/profiles').then(r => r.json());

// 2. User selects profile
const selectedProfile = profiles[0];

// 3. Store in local storage
localStorage.setItem('currentProfile', JSON.stringify(selectedProfile));

// 4. Use profile ID in subsequent requests
const watchlist = await fetch(`/api/profiles/${selectedProfile.id}/watchlist`)
  .then(r => r.json());
```

### Watchlist Management

```javascript
// Add to watchlist
await fetch(`/api/profiles/1/watchlist/123`, {
  method: 'POST'
});

// Check if in watchlist
const { items } = await fetch('/api/profiles/1/watchlist').then(r => r.json());
const isInWatchlist = items.some(item => item.content.id === 123);

// Remove from watchlist
if (isInWatchlist) {
  await fetch(`/api/profiles/1/watchlist/123`, {
    method: 'DELETE'
  });
}
```

## Next Steps

- [Content API](./content.md) - Content discovery
- [Streaming API](./streaming.md) - Watch progress tracking
- [Multi-Profile Feature](../features/multi-profile.md) - Profile system details

**Last Updated**: October 31, 2025
