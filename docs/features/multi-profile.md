# Multi-Profile Support

Personalized viewing experience with individual user profiles.

## Overview

Lanflix supports multiple user profiles, each with independent watch history, watchlists, and preferences.

## Features

### Profile Management

**Create Profiles:**
- Custom name
- Unique avatar colors
- No limit on number of profiles

**Profile Information:**
- Profile name
- Avatar (color-based)
- Creation date
- Watch statistics

### Profile Selection

**Selection Screen:**
- Grid of available profiles
- Click to select
- "Add Profile" option
- Profile management access

**Auto-Selection:**
- Remember last used profile
- Quick switch between profiles
- No authentication required (planned)

### Independent Data

Each profile maintains separate:
- **Watch History** - What you've watched
- **Watch Progress** - Resume points
- **Watchlist** - My List items
- **Preferences** - Settings (planned)
- **Recommendations** - Personalized suggestions (planned)

## Profile Features

### Watch History

Track viewing activity per profile:
- Recently watched content
- Watch timestamps
- Completion status
- Resume points

**History Display:**
```
┌─────────────────────────────────────┐
│  Continue Watching                  │
│  ┌──────┐                           │
│  │Poster│  Inception                │
│  │ 45%  │  45 minutes remaining     │
│  └──────┘  Last watched: 2 hours ago│
└─────────────────────────────────────┘
```

### My List (Watchlist)

Personal collection of saved content:
- Add/remove content
- Sort by date added
- Filter by type
- Quick access

**Watchlist Actions:**
- Add from content detail
- Remove from My List page
- Bulk operations (planned)

### Watch Progress

Automatic progress tracking:
- Resume from last position
- Progress percentage
- Time remaining
- Completion detection

**Progress Sync:**
- Updates every 10 seconds
- Saved on pause/stop
- Synced across devices (planned)

## Profile UI

### Profile Selection Page

```
┌─────────────────────────────────────┐
│  Who's Watching?                    │
│                                     │
│  ┌────────┐  ┌────────┐  ┌────────┐│
│  │  John  │  │  Jane  │  │  Kids  ││
│  │   👤   │  │   👤   │  │   👤   ││
│  └────────┘  └────────┘  └────────┘│
│                                     │
│  ┌────────┐                         │
│  │   +    │  Add Profile            │
│  └────────┘                         │
│                                     │
│  [Manage Profiles]                  │
└─────────────────────────────────────┘
```

### Profile Avatar

Color-based avatar system:
- Primary color (background)
- Secondary color (accent)
- First letter of name
- Circular design

**Avatar Colors:**
```css
.avatar {
  background: linear-gradient(135deg, 
    var(--primary-color), 
    var(--secondary-color)
  );
}
```

### Profile Indicator

Current profile shown in header:
- Avatar icon
- Profile name
- Click to switch

## API Usage

### List Profiles

```javascript
const response = await fetch('/api/profiles');
const { profiles } = await response.json();
```

### Create Profile

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

### Get Profile Watchlist

```javascript
const response = await fetch('/api/profiles/1/watchlist');
const { items } = await response.json();
```

### Update Watch Progress

```javascript
const response = await fetch('/api/stream/123/progress', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    profileId: 1,
    progressSeconds: 3600,
    durationSeconds: 7200
  })
});
```

## Profile Storage

### Local Storage

Current profile stored in browser:
```javascript
// Save selected profile
localStorage.setItem('currentProfile', JSON.stringify({
  id: 1,
  name: 'John',
  avatarColorPrimary: '#FF5733',
  avatarColorSecondary: '#C70039'
}));

// Retrieve current profile
const profile = JSON.parse(localStorage.getItem('currentProfile'));
```

### Database Storage

Profile data in SQLite:
- Profile information
- Watch history entries
- Watchlist items
- Device tokens (for notifications)

## Profile Switching

### Quick Switch

Switch profiles without leaving current page:
1. Click profile avatar in header
2. Select different profile
3. Page reloads with new profile data

### Switch Flow

```
Current Profile → Profile Menu → Select Profile
                                      ↓
                              Update Local Storage
                                      ↓
                              Reload Page Data
                                      ↓
                              New Profile Active
```

## Personalization

### Content Recommendations

Based on profile's watch history:
- Similar content suggestions
- Genre preferences
- Actor/director preferences
- Trending in your interests

### Continue Watching

Profile-specific continue watching:
- Resume from last position
- Sorted by last watched
- Remove watched items
- Episode progression for series

### My List

Personal watchlist:
- Add from any content page
- Quick access from home
- Organized by date added
- Remove completed items

## Profile Management

### Edit Profile

Update profile information:
- Change name
- Update avatar colors
- View statistics (planned)

### Delete Profile

Remove profile and all associated data:
- Watch history deleted
- Watchlist cleared
- Progress removed
- Cannot be undone

**Confirmation Required:**
```
Are you sure you want to delete this profile?
This will permanently delete:
- Watch history
- My List
- Watch progress

[Cancel] [Delete Profile]
```

## Kids Profile (Planned)

Future feature for child-safe viewing:
- Content filtering
- Age restrictions
- Parental controls
- Simplified UI

## Profile Statistics (Planned)

View profile activity:
- Total watch time
- Content watched count
- Favorite genres
- Most watched actors
- Viewing patterns

## Multi-Device Support (Planned)

Sync profiles across devices:
- Cloud sync
- Real-time updates
- Conflict resolution
- Offline support

## Privacy

### Profile Isolation

Profiles are completely isolated:
- No cross-profile data access
- Independent histories
- Separate watchlists
- Private progress tracking

### No Authentication (Current)

Profiles currently have no authentication:
- Anyone can select any profile
- No password protection
- Suitable for trusted environments

### Authentication (Planned)

Future authentication options:
- PIN codes
- Passwords
- Biometric (mobile)
- Session management

## Troubleshooting

### Profile Not Saving

**Check:**
1. Browser local storage enabled
2. Cookies not blocked
3. Browser console for errors
4. Database write permissions

### Watch Progress Not Syncing

**Check:**
1. Profile ID included in requests
2. Network connectivity
3. Backend logs
4. Database integrity

### Watchlist Not Updating

**Check:**
1. Correct profile selected
2. Content exists in library
3. API responses
4. Browser cache

## Next Steps

- [Watch History](./watch-history.md) - Progress tracking
- [Profile API](../api/profile.md) - API reference
- [Content Discovery](./content-discovery.md) - Personalized discovery

**Last Updated**: October 31, 2025
