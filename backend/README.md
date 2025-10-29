# Lanflix Backend Server

Node.js/Express backend for the Lanflix streaming media application.

## Quick Start

1. Install dependencies:
   ```bash
   npm install
   ```

2. Configure environment:
   ```bash
   copy .env.example .env
   # Edit .env with your configuration
   ```

3. Run migrations:
   ```bash
   npm run migrate
   ```

4. Start development server:
   ```bash
   npm run dev
   ```

## Scripts

- `npm run dev` - Start development server with hot reload
- `npm run build` - Build TypeScript to JavaScript
- `npm start` - Start production server
- `npm run migrate` - Run database migrations
- `npm run migrate:undo` - Undo last migration
- `npm run seed` - Seed database with initial data

## Architecture

- **Express.js** - Web framework
- **TypeScript** - Type-safe JavaScript
- **Sequelize** - ORM for SQLite
- **Winston** - Logging
- **Redis** - Caching (optional)

## Directory Structure

- `src/app.ts` - Main application entry point
- `src/config/` - Configuration and environment setup
- `src/routes/` - API route handlers
- `src/services/` - Business logic layer
- `src/models/` - Database models
- `src/clients/` - External API clients
- `src/middleware/` - Express middleware
- `src/utils/` - Utility functions

## Next Steps

See the main project README and `.kiro/specs/streaming-media-server/tasks.md` for the implementation roadmap.
