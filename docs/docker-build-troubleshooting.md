# Docker Build Troubleshooting Guide

## Overview

This guide addresses common Docker build issues and their solutions for the MCP Server .NET project.

## Fixed Issues

### 1. Node.js Installation in BFF Service

**Problem**: The BFF service Docker build was failing due to Node.js installation issues using the deprecated setup script method.

**Solution**: Updated the BFF Dockerfile to use the official Node.js APT repository method with proper certificate handling:

```dockerfile
# Install Node.js 20 LTS using Node.js official distribution
RUN apt-get update && apt-get install -y ca-certificates curl gnupg \
    && mkdir -p /etc/apt/keyrings \
    && curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg \
    && echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_20.x nodistro main" | tee /etc/apt/sources.list.d/nodesource.list \
    && apt-get update && apt-get install -y nodejs \
    && apt-get clean && rm -rf /var/lib/apt/lists/*
```

### 2. Docker Layer Optimization

**Problem**: Inefficient Docker layer caching due to poor instruction ordering.

**Solution**: Reorganized Docker instructions for better layer caching:
- Install system dependencies first
- Copy package files before copying source code
- Install npm dependencies before building React app
- Improved consistency across all Dockerfiles

### 3. Missing npm Dependencies

**Problem**: The local build was failing because npm dependencies were not installed for the React frontend.

**Solution**: 
- Installed npm dependencies locally using `npm ci`
- Updated the BFF project structure to handle npm dependencies properly in Docker builds
- Added proper layering in Dockerfile to cache npm dependencies

## Build Verification

### Local Build Status
- ✅ Solution builds successfully
- ✅ All tests pass (12/12)
- ✅ React frontend builds correctly
- ✅ Health check endpoints work

### Docker Build Status
The Docker builds have been improved to handle:
- Proper Node.js installation
- Better layer caching
- Consistent project structure across services
- Robust dependency installation

## Common Issues and Solutions

### Network Connectivity Issues

If you encounter network connectivity issues during Docker builds:

1. **SSL Certificate Issues**: Ensure proper CA certificates are installed
2. **Proxy Settings**: Configure Docker to use corporate proxies if needed
3. **DNS Resolution**: Verify DNS resolution is working in build environment

### NuGet Restore Issues

If NuGet restore fails:

1. **Check Network Access**: Ensure access to `https://api.nuget.org/v3/index.json`
2. **Authentication**: Verify NuGet authentication if using private feeds
3. **Retry Logic**: Add retry logic for transient network failures

### React Build Issues

If React builds fail:

1. **Node.js Version**: Ensure Node.js 18+ is installed
2. **Memory Issues**: Consider increasing Docker memory limits for large React builds
3. **Dependencies**: Verify all npm dependencies are properly installed

## Best Practices

### Docker Build Optimization

1. **Multi-stage Builds**: Use multi-stage builds to minimize final image size
2. **Layer Caching**: Order instructions from least to most frequently changing
3. **Dependency Installation**: Install dependencies before copying source code
4. **Clean Up**: Remove unnecessary files and packages in same RUN instruction

### CI/CD Integration

1. **Build Cache**: Use GitHub Actions cache for Docker layers
2. **Parallel Builds**: Build multiple services in parallel when possible
3. **Error Handling**: Implement proper error handling and retry logic
4. **Security**: Use minimal base images and scan for vulnerabilities

## Monitoring and Debugging

### Build Logs
- Enable verbose logging for troubleshooting
- Monitor build times and layer sizes
- Check for security vulnerabilities

### Testing
- Test builds in multiple environments
- Verify functionality with integration tests
- Monitor resource usage during builds

## Support

For additional Docker build issues:

1. Check the GitHub Actions workflow logs
2. Verify Docker version compatibility
3. Test builds locally before pushing
4. Consider using Docker Buildx for advanced features