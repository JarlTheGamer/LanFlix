#!/bin/bash
# Lanflix Server - Linux x64 Single Executable Build Script
# This script builds a self-contained single executable for Linux

echo "Building Lanflix Server for Linux x64..."

# Clean previous builds
echo "Cleaning previous builds..."
rm -rf WebApi/bin/Release/net9.0/publish/linux-x64

# Build and publish
echo "Publishing single executable..."
dotnet publish WebApi/Lanflix.WebApi.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    /p:PublishSingleFile=true \
    /p:PublishTrimmed=true \
    /p:TrimMode=partial \
    /p:EnableCompressionInSingleFile=true \
    /p:IncludeNativeLibrariesForSelfExtract=true \
    /p:IncludeAllContentForSelfExtract=true \
    -o "WebApi/bin/Release/net9.0/publish/linux-x64"

if [ $? -eq 0 ]; then
    echo ""
    echo "Build successful!"
    echo "Executable location: WebApi/bin/Release/net9.0/publish/linux-x64/Lanflix.WebApi"
    
    # Display file size
    if [ -f "WebApi/bin/Release/net9.0/publish/linux-x64/Lanflix.WebApi" ]; then
        fileSize=$(du -h "WebApi/bin/Release/net9.0/publish/linux-x64/Lanflix.WebApi" | cut -f1)
        echo "Executable size: $fileSize"
        
        # Make executable
        chmod +x "WebApi/bin/Release/net9.0/publish/linux-x64/Lanflix.WebApi"
    fi
    
    echo ""
    echo "To run the server:"
    echo "  cd WebApi/bin/Release/net9.0/publish/linux-x64"
    echo "  ./Lanflix.WebApi"
else
    echo ""
    echo "Build failed!"
    exit 1
fi
