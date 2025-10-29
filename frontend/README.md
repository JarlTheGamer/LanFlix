# Lanflix Frontend

Cross-platform frontend for the Lanflix streaming media application.

## Quick Start

1. Install dependencies:
   ```bash
   npm install
   ```

2. Start development server:
   ```bash
   npm run dev
   ```

3. Open browser to `http://localhost:5173`

## Scripts

- `npm run dev` - Start Vite development server with hot reload
- `npm run build` - Build for production
- `npm run preview` - Preview production build locally

## Current Structure

### Pages
- `index.html` - Main application page
- `settings.html` - Settings page

### Stylesheets
- `styles.css` - Main stylesheet
- `settings.css` - Settings page stylesheet

### JavaScript Modules
- `main.js` - Main application entry point
- `settings-main.js` - Settings page entry point
- `modules/data.js` - Mock data (will be replaced with API calls)
- `modules/profile-manager.js` - Profile selection and management
- `modules/content-display.js` - Content carousels and hero display
- `modules/navigation.js` - Menu navigation and keyboard controls
- `modules/settings-manager.js` - Settings UI and interactions

### Next Steps (Task 8)

- `modules/api-client.js` - Backend API communication (to be implemented)
- `modules/video-player.js` - Media playback controls (to be implemented)

## Features

- **Profile Selection**: Multi-user support with personalized avatars
- **Hero Carousel**: Featured content with ambilight effects
- **Content Cards**: Expandable movie/series cards with metadata
- **Keyboard Navigation**: Full D-pad/remote control support for TV
- **Settings Management**: Comprehensive settings interface
- **Responsive Design**: Adapts to TV, tablet, and mobile screens

## Development

The frontend is configured to proxy API requests to the backend server at `http://localhost:3000`. Make sure the backend is running when developing.

## Platform Packaging

### Electron (PC)
Coming in Task 11.1

### Capacitor (Android/Android TV)
Coming in Tasks 11.2 and 11.3
