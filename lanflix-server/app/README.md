# Lanflix Server - C# Backend

This is the new high-performance C# backend for Lanflix, built with ASP.NET Core 9.0 following Clean Architecture principles.

## Project Structure

```
Lanflix.Server/
├── Domain/                      # Core business logic (no dependencies)
│   ├── Entities/               # Domain entities
│   ├── ValueObjects/           # Value objects
│   ├── Enums/                  # Enumerations
│   ├── Interfaces/             # Domain interfaces
│   └── Common/                 # Common domain types
│
├── Application/                 # Application business logic (depends on Domain)
│   ├── Common/
│   │   ├── Interfaces/         # Application interfaces
│   │   ├── DTOs/               # Data Transfer Objects
│   │   ├── Behaviors/          # MediatR pipeline behaviors
│   │   ├── Exceptions/         # Application exceptions
│   │   └── Mappings/           # Object mappings
│   └── Features/               # CQRS features (vertical slices)
│       ├── Library/            # Library management
│       ├── Streaming/          # Streaming functionality
│       ├── Profiles/           # User profiles
│       └── Metadata/           # Metadata management
│
├── Infrastructure/              # External concerns (depends on Application)
│   ├── Persistence/
│   │   ├── Configurations/     # EF Core entity configurations
│   │   └── Repositories/       # Repository implementations
│   ├── Services/
│   │   ├── FFmpeg/             # FFmpeg integration
│   │   ├── Caching/            # Caching services
│   │   ├── BackgroundJobs/     # Hangfire jobs
│   │   └── ExternalApis/       # External API clients
│   └── Migration/              # Legacy data migration
│
└── WebApi/                      # API layer (depends on all)
    ├── Controllers/            # API controllers
    ├── Hubs/                   # SignalR hubs
    ├── Middleware/             # Custom middleware
    └── Filters/                # Action filters

```

## Technology Stack

- **Framework**: ASP.NET Core 9.0
- **Database**: Entity Framework Core (SQLite/PostgreSQL)
- **CQRS**: MediatR
- **Validation**: FluentValidation
- **FFmpeg**: Xabe.FFmpeg
- **Caching**: Redis + Memory Cache
- **Logging**: Serilog
- **Background Jobs**: Hangfire
- **Real-time**: SignalR

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- FFmpeg installed and in PATH
- Redis (optional, for distributed caching)

### Build

```bash
dotnet restore
dotnet build
```

### Run

```bash
cd WebApi
dotnet run
```

The API will be available at `https://localhost:5001` and `http://localhost:5000`.

## Clean Architecture Principles

1. **Domain Layer**: Contains core business entities and logic. No external dependencies.
2. **Application Layer**: Contains application business logic, CQRS handlers, and interfaces. Depends only on Domain.
3. **Infrastructure Layer**: Implements interfaces defined in Application. Contains EF Core, external services, etc.
4. **WebApi Layer**: Entry point. Contains controllers, middleware, and configuration.

## Development Guidelines

- Follow CQRS pattern for all features
- Use MediatR for command/query handling
- Implement FluentValidation for input validation
- Use async/await throughout
- Apply repository pattern only when needed
- Keep controllers thin - delegate to MediatR handlers
