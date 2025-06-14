# Docker Build Troubleshooting Guide

## Overview

This guide addresses common Docker build issues and their solutions for the MCP Server .NET project.

## Latest Improvements (Current Version)

### 1. Simplified Docker Build Pipeline

**Problem**: Complex retry logic and shell commands were causing container runtime issues like `runc run failed: unable to start container process: error updating spec state: invalid state transition from stopped to paused`.

**Solution**: Simplified Dockerfiles and enhanced GitHub Actions pipeline:

#### GitHub Actions Improvements:
- **Pre-caching**: Added pre-restoration of .NET packages and npm dependencies on the runner before Docker builds
- **Network Configuration**: Added `network: host` to Docker builds for improved connectivity
- **Action Caching**: Implemented comprehensive caching for NuGet packages and npm dependencies
- **Multi-step Setup**: Setup .NET and Node.js on the runner to validate dependencies before Docker builds

```yaml
- name: Cache NuGet packages
  uses: actions/cache@v4
  with:
    path: ~/.nuget/packages
    key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}

- name: Pre-restore .NET packages
  run: dotnet restore --verbosity normal
```

#### Dockerfile Simplifications:
- **Removed Complex Shell Scripts**: Eliminated multi-line retry commands that caused container runtime issues
- **Single-stage Commands**: Used simple, reliable RUN commands instead of complex conditional logic
- **Standard Parameters**: Used `--disable-parallel --no-cache` for more reliable package restoration

```dockerfile
# Simplified, reliable approach
RUN dotnet restore "src/Presentation/McpServer.Api/McpServer.Api.csproj" \
    --verbosity normal \
    --disable-parallel \
    --no-cache
```

### 2. Network Connectivity Strategy

**Previous Issue**: Network connectivity to NuGet and npm registries failing in CI environments.

**Current Approach**:
1. **Pre-download on Runner**: Download packages on GitHub Actions runner where network is more reliable
2. **Docker Build Optimization**: Use Docker layer caching and simplified commands
3. **Host Network Mode**: Enable host networking for Docker builds to bypass container network restrictions

## Fixed Issues (Previous Versions)

### 1. NuGet Restore Network Connectivity Issues

**Problem**: Docker builds failing with `NU1301: Unable to load the service index for source https://api.nuget.org/v3/index.json` due to network connectivity issues in CI/CD environments.

**Previous Solution**: Retry logic (now replaced with pre-caching approach)

### 2. Node.js Installation in BFF Service

**Problem**: The BFF service Docker build was failing due to Node.js installation issues.

**Solution**: Updated to use official Node.js APT repository:

```dockerfile
# Install Node.js 20 LTS
RUN apt-get update && apt-get install -y ca-certificates curl gnupg \
    && mkdir -p /etc/apt/keyrings \
    && curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg \
    && echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_20.x nodistro main" | tee /etc/apt/sources.list.d/nodesource.list \
    && apt-get update && apt-get install -y nodejs
```

## Current Build Status

### Verification Steps
- ✅ Solution builds successfully locally
- ✅ All tests pass (12/12)
- ✅ React frontend builds correctly
- ✅ Health check endpoints work
- ✅ Simplified Dockerfiles with reliable commands
- ✅ Enhanced GitHub Actions pipeline with pre-caching

## Common Issues and Solutions

### Network Connectivity Issues

If you encounter network connectivity issues during Docker builds:

1. **Use GitHub Actions Caching**: The pipeline now pre-downloads dependencies
2. **Test Locally**: Build locally to isolate CI-specific issues
3. **Check Runner Status**: Verify GitHub Actions runner has proper network access

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

3. **Test Individual Services**:
   ```bash
   # API only
   docker build -f ./src/Presentation/McpServer.Api/Dockerfile -t test-api .
   
   # BFF (includes Node.js)
   docker build -f ./src/Presentation/McpServer.Bff/Dockerfile -t test-bff .
   
   # Host
   docker build -f ./src/Host/McpServer.Host/Dockerfile -t test-host .
   ```

## Best Practices (Current)

### Docker Build Optimization

1. **Simplified Commands**: Use straightforward RUN commands without complex shell logic
2. **Layer Caching**: Order instructions from least to most frequently changing
3. **Pre-validation**: Test dependencies on CI runner before Docker builds
4. **Host Networking**: Use host network mode in CI for better connectivity
5. **Progressive Building**: Build images independently to isolate issues

### CI/CD Integration

1. **Action Caching**: Use GitHub Actions cache for all dependencies
2. **Pre-setup**: Setup .NET/Node.js on runner before Docker builds
3. **Error Isolation**: Build each Docker image separately to identify specific issues
4. **Timeout Management**: Use appropriate timeouts for network operations

## Environment-Specific Solutions

### Development Environment
```bash
# Build locally with debug output
docker build --progress=plain -f ./src/Presentation/McpServer.Api/Dockerfile -t mcp-server-api .
```

### CI/CD Environment
- Pre-cached dependencies reduce network dependency during Docker builds
- Host networking improves connectivity reliability
- Individual service builds enable better error isolation
- Comprehensive caching reduces build times

## Support

For additional Docker build issues:

1. Check GitHub Actions logs for specific error details
2. Test builds locally to compare with CI behavior
3. Verify the pre-caching steps completed successfully
4. Review network connectivity on the GitHub Actions runner
5. Consider using self-hosted runners for consistent network access