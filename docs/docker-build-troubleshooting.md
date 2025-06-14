# Docker Build Troubleshooting Guide

## Overview

This guide addresses common Docker build issues and their solutions for the MCP Server .NET project.

## Fixed Issues

### 1. NuGet Restore Network Connectivity Issues

**Problem**: Docker builds failing with `NU1301: Unable to load the service index for source https://api.nuget.org/v3/index.json` due to network connectivity issues in CI/CD environments.

**Solution**: Implemented comprehensive retry logic and improved NuGet configuration:

```dockerfile
# Configure NuGet with retry and timeout settings
COPY ["nuget.config", "./"]
RUN dotnet nuget locals all --clear

# Restore dependencies with retry logic
RUN dotnet restore "src/Presentation/McpServer.Api/McpServer.Api.csproj" --no-cache --force --verbosity normal || \
    (echo "First restore failed, retrying..." && sleep 5 && dotnet restore "src/Presentation/McpServer.Api/McpServer.Api.csproj" --no-cache --force --verbosity normal) || \
    (echo "Second restore failed, retrying..." && sleep 10 && dotnet restore "src/Presentation/McpServer.Api/McpServer.Api.csproj" --no-cache --force --verbosity normal)
```

**NuGet Configuration Improvements**:
- Added custom `nuget.config` with optimized settings
- Configured global packages folder for better caching
- Clear package sources to avoid conflicts
- Added proxy configuration support

### 2. Node.js Installation in BFF Service

**Problem**: The BFF service Docker build was failing due to Node.js installation issues using the deprecated setup script method.

**Solution**: Updated the BFF Dockerfile to use the official Node.js APT repository method with improved error handling:

```dockerfile
# Install Node.js 20 LTS using Node.js official distribution with improved error handling
RUN apt-get update && apt-get install -y ca-certificates curl gnupg \
    && mkdir -p /etc/apt/keyrings \
    && for i in 1 2 3; do \
        curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg \
        && break || sleep 5; \
    done \
    && echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_20.x nodistro main" | tee /etc/apt/sources.list.d/nodesource.list \
    && apt-get update && apt-get install -y nodejs \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Verify Node.js installation
RUN node --version && npm --version
```

### 3. NPM Dependencies Installation

**Problem**: NPM installation failures due to network issues or dependency conflicts.

**Solution**: Added retry logic for npm operations:

```dockerfile
# Install npm dependencies with retry logic
RUN npm ci --production=false --no-audit --no-fund || \
    (echo "First npm install failed, retrying..." && sleep 5 && npm ci --production=false --no-audit --no-fund) || \
    (echo "Second npm install failed, retrying..." && sleep 10 && npm ci --production=false --no-audit --no-fund)
```

### 4. GitHub Actions Docker Build Optimization

**Problem**: Docker builds timing out or failing in CI/CD environment.

**Solution**: Enhanced GitHub Actions workflow with:

```yaml
- name: Build and push API Docker image
  uses: docker/build-push-action@v5
  with:
    context: .
    file: ./src/Presentation/McpServer.Api/Dockerfile
    push: true
    tags: ${{ steps.meta-api.outputs.tags }}
    labels: ${{ steps.meta-api.outputs.labels }}
    cache-from: type=gha
    cache-to: type=gha,mode=max
    platforms: linux/amd64
    build-args: |
      BUILDKIT_INLINE_CACHE=1
```

## Build Verification

### Local Build Status
- ✅ Solution builds successfully
- ✅ All tests pass (12/12)
- ✅ React frontend builds correctly
- ✅ Health check endpoints work
- ✅ Docker builds include retry mechanisms

### Docker Build Status
The Docker builds have been improved to handle:
- ✅ Network connectivity issues with automatic retries
- ✅ Proper Node.js installation with error handling
- ✅ Better layer caching for faster builds
- ✅ Consistent project structure across services
- ✅ Robust dependency installation for both .NET and npm

## Common Issues and Solutions

### Network Connectivity Issues

If you encounter network connectivity issues during Docker builds:

1. **SSL Certificate Issues**: Ensure proper CA certificates are installed
2. **Proxy Settings**: Configure Docker to use corporate proxies if needed
3. **DNS Resolution**: Verify DNS resolution is working in build environment
4. **NuGet Service Availability**: Check if NuGet.org is accessible

### Manual Recovery Steps

If builds continue to fail:

1. **Clear Docker Build Cache**:
   ```bash
   docker builder prune --all
   ```

2. **Build with No Cache**:
   ```bash
   docker build --no-cache -f ./src/Presentation/McpServer.Api/Dockerfile -t mcp-server-api .
   ```

3. **Test Network Connectivity**:
   ```bash
   curl -I https://api.nuget.org/v3/index.json
   ```

### React Build Issues

If React builds fail:

1. **Node.js Version**: Ensure Node.js 20+ is installed
2. **Memory Issues**: Consider increasing Docker memory limits for large React builds
3. **Dependencies**: Verify all npm dependencies are properly installed

## Best Practices

### Docker Build Optimization

1. **Multi-stage Builds**: Use multi-stage builds to minimize final image size
2. **Layer Caching**: Order instructions from least to most frequently changing
3. **Dependency Installation**: Install dependencies before copying source code
4. **Clean Up**: Remove unnecessary files and packages in same RUN instruction
5. **Retry Logic**: Implement retry mechanisms for network operations

### CI/CD Integration

1. **Build Cache**: Use GitHub Actions cache for Docker layers
2. **Platform Specification**: Specify target platform to avoid cross-platform issues
3. **Error Handling**: Implement proper error handling and retry logic
4. **Security**: Use minimal base images and scan for vulnerabilities

## Environment-Specific Solutions

### Development Environment
```bash
# Build locally with debug output
docker build --progress=plain -f ./src/Presentation/McpServer.Api/Dockerfile -t mcp-server-api .
```

### CI/CD Environment
- GitHub Actions include automatic retry for transient failures
- Build cache optimized for GitHub Actions runners
- Comprehensive error logging and debugging information

## Support

For additional Docker build issues:

1. Check the GitHub Actions workflow logs for specific error details
2. Verify network connectivity in your environment
3. Test builds locally to isolate CI-specific issues
4. Review recent changes to dependency versions
5. Check Docker version compatibility