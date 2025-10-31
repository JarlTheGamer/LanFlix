# Mobile App Architecture & Quality Plan

## Current State Analysis

### Issues Identified

1. **Architecture Problems**
   - Capacitor wrapper around web app (not native performance)
   - No proper mobile-first UI/UX design
   - Desktop web UI forced into mobile viewport
   - No offline-first architecture
   - Missing mobile-specific optimizations

2. **Build System Issues**
   - Complex nested build structure (`build-tools/android/android`)
   - Manual Gradle commands required
   - No CI/CD pipeline
   - No automated testing
   - Build scripts in `.bat` files (Windows-only)

3. **Code Quality Issues**
   - No TypeScript in frontend modules
   - No component architecture
   - jQuery-style DOM manipulation
   - No state management
   - No mobile-specific modules

4. **Missing Mobile Features**
   - No offline playback
   - No download queue management
   - No background downloads
   - No push notifications
   - No biometric authentication
   - No picture-in-picture
   - No casting integration
   - No mobile gestures (swipe, pinch-to-zoom)

5. **Performance Issues**
   - No lazy loading
   - No image optimization
   - No code splitting
   - Large bundle size
   - No service worker/PWA features

## Recommended Architecture

### Option 1: React Native (Recommended)

**Pros:**
- True native performance
- Large ecosystem and community
- Expo for rapid development
- React Native Video for optimized playback
- Easy to implement offline-first
- Better developer experience
- Hot reload during development
- Native modules for advanced features

**Cons:**
- Complete rewrite required
- Learning curve if team unfamiliar with React

**Tech Stack:**
```
- React Native 0.73+
- TypeScript
- React Navigation 6
- React Query (data fetching/caching)
- Zustand (state management)
- React Native Video
- React Native MMKV (fast storage)
- React Native Gesture Handler
- Expo (optional, for easier development)
```

### Option 2: Flutter

**Pros:**
- Single codebase for iOS/Android/Web
- Excellent performance
- Beautiful UI out of the box
- Strong typing with Dart
- Hot reload

**Cons:**
- Team needs to learn Dart
- Smaller ecosystem than React Native
- Video playback plugins less mature

### Option 3: Improved Capacitor (Quick Fix)

**Pros:**
- Minimal rewrite
- Keep existing web codebase
- Faster to implement

**Cons:**
- Still web-based performance
- Limited native capabilities
- Not truly mobile-optimized

## Recommended Approach: React Native

### Phase 1: Foundation (Weeks 1-2)

**1.1 Project Setup**
```bash
npx react-native init LanflixMobile --template react-native-template-typescript
```

**1.2 Core Dependencies**
```json
{
  "dependencies": {
    "react": "18.2.0",
    "react-native": "0.73.0",
    "@react-navigation/native": "^6.1.9",
    "@react-navigation/stack": "^6.3.20",
    "@react-navigation/bottom-tabs": "^6.5.11",
    "@tanstack/react-query": "^5.0.0",
    "zustand": "^4.4.7",
    "react-native-video": "^6.0.0",
    "react-native-mmkv": "^2.11.0",
    "axios": "^1.6.0",
    "@react-native-async-storage/async-storage": "^1.21.0",
    "react-native-gesture-handler": "^2.14.0",
    "react-native-reanimated": "^3.6.0"
  }
}
```

**1.3 Folder Structure**
```
mobile/
├── src/
│   ├── api/              # API client & endpoints
│   ├── components/       # Reusable components
│   │   ├── common/       # Buttons, inputs, cards
│   │   ├── media/        # Video player, thumbnails
│   │   └── navigation/   # Nav components
│   ├── screens/          # Screen components
│   │   ├── auth/
│   │   ├── home/
│   │   ├── player/
│   │   ├── library/
│   │   └── profile/
│   ├── hooks/            # Custom React hooks
│   ├── store/            # Zustand stores
│   ├── services/         # Business logic
│   ├── utils/            # Helpers
│   ├── types/            # TypeScript types
│   ├── constants/        # App constants
│   └── navigation/       # Navigation config
├── android/              # Android native code
├── ios/                  # iOS native code
└── __tests__/            # Tests
```

### Phase 2: Core Features (Weeks 3-4)

**2.1 Authentication & Profiles**
- Profile selection screen
- PIN/biometric authentication
- Secure token storage (MMKV)
- Auto-login with saved credentials

**2.2 Home & Discovery**
- Horizontal scrolling carousels
- Lazy loading with pagination
- Pull-to-refresh
- Search with debouncing
- Filter/sort options

**2.3 Video Player**
- React Native Video integration
- Custom controls (play, pause, seek)
- Brightness/volume gestures
- Picture-in-picture support
- Subtitle support
- Quality selection
- Playback speed control

### Phase 3: Advanced Features (Weeks 5-6)

**3.1 Offline Support**
- Download queue management
- Background downloads
- Storage management
- Offline playback
- Download progress tracking

**3.2 Casting & External Display**
- Chromecast integration
- AirPlay support
- HDMI output detection

**3.3 Notifications**
- Push notifications (new content)
- Download completion alerts
- Playback reminders

### Phase 4: Polish & Optimization (Weeks 7-8)

**4.1 Performance**
- Image caching and optimization
- Code splitting
- Bundle size optimization
- Memory leak prevention
- Smooth 60fps animations

**4.2 Testing**
- Unit tests (Jest)
- Integration tests
- E2E tests (Detox)
- Performance testing

**4.3 CI/CD**
- GitHub Actions workflow
- Automated builds
- Automated testing
- Beta distribution (TestFlight/Play Console)

## Code Quality Standards

### 1. TypeScript Strict Mode
```typescript
// tsconfig.json
{
  "compilerOptions": {
    "strict": true,
    "noImplicitAny": true,
    "strictNullChecks": true,
    "strictFunctionTypes": true
  }
}
```

### 2. ESLint Configuration
```javascript
module.exports = {
  extends: [
    '@react-native-community',
    'plugin:@typescript-eslint/recommended',
    'plugin:react-hooks/recommended'
  ],
  rules: {
    'no-console': 'warn',
    '@typescript-eslint/no-unused-vars': 'error',
    'react-hooks/exhaustive-deps': 'error'
  }
};
```

### 3. Component Standards
```typescript
// Example: Proper component structure
import React, { memo } from 'react';
import { StyleSheet, View, Text } from 'react-native';

interface MediaCardProps {
  title: string;
  thumbnail: string;
  onPress: () => void;
}

export const MediaCard = memo<MediaCardProps>(({ title, thumbnail, onPress }) => {
  return (
    <View style={styles.container}>
      <Text>{title}</Text>
    </View>
  );
});

const styles = StyleSheet.create({
  container: {
    padding: 16,
  },
});
```

### 4. API Client Pattern
```typescript
// src/api/client.ts
import axios from 'axios';
import { MMKV } from 'react-native-mmkv';

const storage = new MMKV();

export const apiClient = axios.create({
  baseURL: storage.getString('serverUrl') || 'http://localhost:3000',
  timeout: 10000,
});

apiClient.interceptors.request.use((config) => {
  const token = storage.getString('authToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});
```

### 5. State Management Pattern
```typescript
// src/store/profileStore.ts
import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';
import AsyncStorage from '@react-native-async-storage/async-storage';

interface ProfileState {
  currentProfile: Profile | null;
  setProfile: (profile: Profile) => void;
  clearProfile: () => void;
}

export const useProfileStore = create<ProfileState>()(
  persist(
    (set) => ({
      currentProfile: null,
      setProfile: (profile) => set({ currentProfile: profile }),
      clearProfile: () => set({ currentProfile: null }),
    }),
    {
      name: 'profile-storage',
      storage: createJSONStorage(() => AsyncStorage),
    }
  )
);
```

## Testing Strategy

### Unit Tests
```typescript
// __tests__/components/MediaCard.test.tsx
import React from 'react';
import { render, fireEvent } from '@testing-library/react-native';
import { MediaCard } from '../src/components/MediaCard';

describe('MediaCard', () => {
  it('renders correctly', () => {
    const { getByText } = render(
      <MediaCard title="Test Movie" thumbnail="" onPress={() => {}} />
    );
    expect(getByText('Test Movie')).toBeTruthy();
  });

  it('calls onPress when tapped', () => {
    const onPress = jest.fn();
    const { getByText } = render(
      <MediaCard title="Test Movie" thumbnail="" onPress={onPress} />
    );
    fireEvent.press(getByText('Test Movie'));
    expect(onPress).toHaveBeenCalled();
  });
});
```

### E2E Tests
```typescript
// e2e/home.test.ts
describe('Home Screen', () => {
  beforeAll(async () => {
    await device.launchApp();
  });

  it('should show home screen after login', async () => {
    await element(by.id('profile-selector')).tap();
    await element(by.id('profile-1')).tap();
    await expect(element(by.id('home-screen'))).toBeVisible();
  });

  it('should play video when tapped', async () => {
    await element(by.id('media-card-1')).tap();
    await expect(element(by.id('video-player'))).toBeVisible();
  });
});
```

## Build & Deployment

### Android Build Script
```bash
#!/bin/bash
# scripts/build-android.sh

# Clean
cd android && ./gradlew clean

# Build release APK
./gradlew assembleRelease

# Build AAB for Play Store
./gradlew bundleRelease

echo "Build complete!"
echo "APK: android/app/build/outputs/apk/release/app-release.apk"
echo "AAB: android/app/build/outputs/bundle/release/app-release.aab"
```

### CI/CD Pipeline (GitHub Actions)
```yaml
# .github/workflows/mobile-ci.yml
name: Mobile CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
        with:
          node-version: 18
      - run: npm ci
      - run: npm test
      - run: npm run lint

  build-android:
    runs-on: ubuntu-latest
    needs: test
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-java@v3
        with:
          distribution: 'temurin'
          java-version: '17'
      - run: npm ci
      - run: cd android && ./gradlew assembleRelease
      - uses: actions/upload-artifact@v3
        with:
          name: android-apk
          path: android/app/build/outputs/apk/release/
```

## Migration Strategy

### Option A: Parallel Development (Recommended)
1. Keep existing Capacitor app running
2. Build React Native app in parallel
3. Feature parity testing
4. Gradual rollout to beta users
5. Full migration after stability proven

### Option B: Incremental Migration
1. Start with new screens in React Native
2. Use React Native's ability to embed in existing apps
3. Migrate screen by screen
4. Complete migration over 3-6 months

## Success Metrics

- App launch time < 2 seconds
- 60fps scrolling and animations
- Video playback starts < 1 second
- Crash-free rate > 99.5%
- App size < 50MB
- Memory usage < 200MB during playback
- Battery drain < 5% per hour of playback

## Timeline Summary

- **Weeks 1-2:** Setup & foundation
- **Weeks 3-4:** Core features
- **Weeks 5-6:** Advanced features
- **Weeks 7-8:** Polish & testing
- **Week 9:** Beta release
- **Week 10:** Production release

## Next Steps

1. Review and approve architecture
2. Set up React Native project
3. Create design system/UI kit
4. Begin Phase 1 implementation
5. Set up CI/CD pipeline
6. Establish code review process
