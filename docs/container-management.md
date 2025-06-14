# MCP Gateway Container Management Guide

This guide provides instructions for building, testing, and managing the MCP Gateway container for Model Context Protocol compliance.

## Overview

The MCP Gateway is a containerized version of the MCP Server .NET API service, optimized for production deployment with enhanced security and performance characteristics.

## Container Features

### Security Hardening
- Non-root user execution (user ID 1000)
- Read-only root filesystem
- Dropped capabilities
- Minimal attack surface

### Performance Optimization
- Multi-stage build for smaller image size
- Optimized .NET runtime configuration
- Efficient layer caching
- Health check integration

### Protocol Compliance
- Full MCP protocol endpoint support
- Comprehensive health checks
- Proper error handling
- Protocol validation tests

## Building the Container

### Prerequisites
- Docker 20.10+
- .NET 8.0 SDK
- Published application binaries

### Build Process

1. **Publish the application**:
```bash
dotnet publish src/Presentation/McpServer.Api/McpServer.Api.csproj \
  -c Release \
  -o ./publish/api \
  --self-contained false
```

2. **Build the container**:
```bash
docker build -f Dockerfile.gateway -t mcp-gateway:latest .
```

3. **Tag for registry**:
```bash
docker tag mcp-gateway:latest ghcr.io/stig-johnny/mcp-server-dotnet/mcp-gateway:latest
```

### Automated Build (GitHub Actions)

The container is automatically built and pushed on:
- Push to `main` branch
- Push to `develop` branch  
- Tagged releases (`v*`)

```yaml
# .github/workflows/docker-build.yml includes gateway build
- name: Build and push Gateway Docker image
  uses: docker/build-push-action@v5
  with:
    context: .
    file: ./Dockerfile.gateway
    push: ${{ github.event_name != 'pull_request' }}
    tags: ${{ steps.meta-gateway.outputs.tags }}
```

## Testing the Container

### Local Testing

1. **Run the container**:
```bash
docker run --rm -p 8080:8080 mcp-gateway:latest
```

2. **Test health endpoints**:
```bash
# Health check
curl http://localhost:8080/health

# Readiness check
curl http://localhost:8080/health/ready
```

3. **Test MCP protocol endpoints**:
```bash
# List available tools
curl http://localhost:8080/api/mcp/tools

# List available resources
curl http://localhost:8080/api/mcp/resources

# Execute a tool
curl -X POST http://localhost:8080/api/mcp/tools/echo/execute \
  -H "Content-Type: application/json" \
  -d '{"message": "Hello MCP!"}'
```

### Protocol Compliance Testing

Run the comprehensive test suite:
```bash
dotnet test tests/McpServer.Tests --filter McpProtocolComplianceTests
```

This validates:
- ✅ Health check endpoints
- ✅ MCP tool listing and execution
- ✅ MCP resource access
- ✅ Error handling
- ✅ Response format compliance
- ✅ HTTP header validation

## Deployment Configurations

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Runtime environment |
| `ASPNETCORE_URLS` | `http://+:8080` | Listening URLs |
| `MCP_GATEWAY_MODE` | `true` | Gateway-specific configuration |
| `DOTNET_RUNNING_IN_CONTAINER` | `true` | Container optimization |

### Resource Requirements

#### Minimum (Development)
- CPU: 250m
- Memory: 256Mi

#### Recommended (Production)
- CPU: 500m
- Memory: 512Mi

#### High Load (Production)
- CPU: 1000m
- Memory: 1Gi

### Health Checks

The container includes comprehensive health checks:

```yaml
livenessProbe:
  httpGet:
    path: /health
    port: 8080
  initialDelaySeconds: 30
  periodSeconds: 15
  timeoutSeconds: 10
  failureThreshold: 3

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 10
  timeoutSeconds: 5
  failureThreshold: 2
```

## Monitoring and Observability

### Metrics Endpoints

- `/health` - Basic health status
- `/health/ready` - Readiness for traffic
- `/metrics` - Prometheus metrics (if enabled)

### Logging

The container outputs structured logs to stdout in JSON format, including:
- Request/response logging
- Tool execution events
- Resource access events
- Error and warning messages

### Distributed Tracing

Integration with OpenTelemetry for distributed tracing across microservices.

## Production Deployment

### Kubernetes Deployment

Use the provided Helm charts:

```bash
# Gateway-focused deployment
helm install mcp-gateway ./helm/mcp-server-dotnet \
  --values helm/mcp-server-dotnet/values-gateway.yaml \
  --namespace mcp-gateway \
  --create-namespace
```

### Docker Compose (Development)

```yaml
version: '3.8'
services:
  mcp-gateway:
    image: ghcr.io/stig-johnny/mcp-server-dotnet/mcp-gateway:main
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
```

## Troubleshooting

### Common Issues

#### Container Won't Start
1. Check resource limits
2. Verify port availability
3. Review environment variables
4. Check application logs

#### Health Checks Failing
1. Verify endpoints are responding
2. Check network connectivity
3. Review timeout settings
4. Examine application startup time

#### Performance Issues
1. Monitor resource usage
2. Review garbage collection settings
3. Check for memory leaks
4. Analyze request patterns

### Debug Commands

```bash
# Container logs
docker logs <container-id>

# Resource usage
docker stats <container-id>

# Execute shell in container
docker exec -it <container-id> /bin/sh

# Health check from inside container
docker exec <container-id> curl -f http://localhost:8080/health
```

## Security Considerations

### Container Security
- Runs as non-root user (UID 1000)
- Read-only root filesystem
- Minimal base image (aspnet:8.0)
- No unnecessary packages or tools

### Network Security
- Only exposes port 8080
- HTTPS redirection supported
- Rate limiting configured in ingress
- Network policies can be applied

### Secrets Management
- No secrets baked into image
- Environment variables for configuration
- Kubernetes secrets for sensitive data
- Support for Azure Key Vault integration

## Updates and Maintenance

### Container Updates
1. Update base image regularly
2. Rebuild with latest .NET patches
3. Test thoroughly before deployment
4. Use rolling updates in Kubernetes

### Backup and Recovery
- Stateless application design
- Configuration in external systems
- Database backups (if applicable)
- Disaster recovery procedures

## Support and Contact

For issues related to the MCP Gateway container:
1. Check this documentation
2. Review GitHub issues
3. Contact the development team
4. Submit bug reports with logs and configuration