# NuGet Packages Reference

## Domain Layer
- **No external dependencies** (Clean Architecture principle)

## Application Layer
- **MediatR** (12.4.1) - CQRS pattern implementation
- **FluentValidation** (11.11.0) - Input validation
- **FluentValidation.DependencyInjectionExtensions** (11.11.0) - DI integration
- **Microsoft.Extensions.Caching.Abstractions** (9.0.0) - Caching interfaces
- **Microsoft.Extensions.Logging.Abstractions** (9.0.0) - Logging interfaces

## Infrastructure Layer

### Database
- **Microsoft.EntityFrameworkCore** (9.0.0) - ORM framework
- **Microsoft.EntityFrameworkCore.Sqlite** (9.0.0) - SQLite provider
- **Microsoft.EntityFrameworkCore.Design** (9.0.0) - Design-time tools
- **Npgsql.EntityFrameworkCore.PostgreSQL** (9.0.0) - PostgreSQL provider
- **Dapper** (2.1.35) - Micro-ORM for performance-critical queries

### FFmpeg Integration
- **Xabe.FFmpeg** (6.0.2) - FFmpeg wrapper for .NET

### Caching
- **StackExchange.Redis** (2.8.16) - Redis client
- **Microsoft.Extensions.Caching.StackExchangeRedis** (9.0.0) - Redis cache provider

### Real-time Communication
- **Microsoft.AspNetCore.SignalR.Client** (9.0.0) - SignalR client

### Background Jobs
- **Hangfire.AspNetCore** (1.8.17) - Background job processing
- **Hangfire.MemoryStorage** (1.8.1.1) - In-memory storage for Hangfire

### HTTP Client
- **Microsoft.Extensions.Http** (9.0.0) - HTTP client factory

## WebApi Layer
- **Microsoft.AspNetCore.OpenApi** (9.0.10) - OpenAPI/Swagger support
- **Serilog.AspNetCore** (9.0.0) - Structured logging
- **Serilog.Sinks.File** (7.0.0) - File logging sink
- **Microsoft.AspNetCore.Authentication.JwtBearer** (9.0.0) - JWT authentication

## Additional Packages to Consider (Future Tasks)

### Performance
- **System.Threading.Channels** - Built-in, for high-performance async pipelines
- **System.IO.Pipelines** - Built-in, for efficient I/O operations
- **Microsoft.Extensions.ObjectPool** - Built-in, for object pooling

### Monitoring
- **OpenTelemetry.Extensions.Hosting** - Distributed tracing
- **OpenTelemetry.Instrumentation.AspNetCore** - ASP.NET Core instrumentation
- **OpenTelemetry.Instrumentation.Http** - HTTP client instrumentation
- **OpenTelemetry.Instrumentation.EntityFrameworkCore** - EF Core instrumentation

### Testing (Future)
- **xUnit** - Testing framework
- **FluentAssertions** - Fluent assertion library
- **Moq** - Mocking framework
- **Microsoft.AspNetCore.Mvc.Testing** - Integration testing
- **Testcontainers** - Docker containers for testing
