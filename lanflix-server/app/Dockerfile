# Lanflix Server - Multi-stage Docker Build
# This Dockerfile creates an optimized production image with FFmpeg support

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Lanflix.Server.sln", "./"]
COPY ["Domain/Lanflix.Domain.csproj", "Domain/"]
COPY ["Application/Lanflix.Application.csproj", "Application/"]
COPY ["Infrastructure/Lanflix.Infrastructure.csproj", "Infrastructure/"]
COPY ["WebApi/Lanflix.WebApi.csproj", "WebApi/"]

# Restore dependencies
RUN dotnet restore "Lanflix.Server.sln"

# Copy all source code
COPY . .

# Build and publish
WORKDIR "/src/WebApi"
RUN dotnet publish "Lanflix.WebApi.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install FFmpeg and dependencies
RUN apt-get update && \
    apt-get install -y \
        ffmpeg \
        libva-drm2 \
        libva2 \
        i965-va-driver \
        intel-media-va-driver \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN groupadd -r lanflix && useradd -r -g lanflix lanflix

# Create directories for media and data
RUN mkdir -p /app/data /app/media /app/logs /app/temp && \
    chown -R lanflix:lanflix /app

# Copy published application
COPY --from=build /app/publish .

# Set ownership
RUN chown -R lanflix:lanflix /app

# Switch to non-root user
USER lanflix

# Expose ports
EXPOSE 5000
EXPOSE 5001

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000;https://+:5001
ENV ASPNETCORE_ENVIRONMENT=Production
ENV Lanflix__MediaPaths__Movies=/app/media/movies
ENV Lanflix__MediaPaths__Series=/app/media/series
ENV Lanflix__MediaPaths__PosterCache=/app/data/cache/posters
ENV Lanflix__MediaPaths__BackdropCache=/app/data/cache/backdrops
ENV Lanflix__Transcoding__TempPath=/app/temp
ENV ConnectionStrings__DefaultConnection=Data Source=/app/data/lanflix.db

# Entry point
ENTRYPOINT ["dotnet", "Lanflix.WebApi.dll"]
