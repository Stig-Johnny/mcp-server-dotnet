# MCP Server .NET - Kubernetes Deployment Guide

This guide provides instructions for deploying the MCP Server .NET application to a Kubernetes cluster using ArgoCD and Helm, with a focus on the MCP Gateway for protocol compliance.

> 📖 **For comprehensive ArgoCD automation**: See the detailed [**ArgoCD Deployment Guide**](argocd-deployment.md) for complete application manifest creation, sync policies configuration, and status verification procedures.

## Prerequisites

- Kubernetes cluster (1.19+)
- ArgoCD installed in the cluster
- Helm 3.x
- kubectl configured to access your cluster
- Container registry access (GitHub Container Registry is used by default)

## Architecture Overview

The deployment consists of:

- **MCP Gateway**: Primary service for Model Context Protocol compliance (`mcp-gateway`)
- **API Service**: Legacy MCP Server API (`McpServer.Api`)
- **BFF Service**: Backend-for-Frontend with React frontend (`McpServer.Bff`)
- **Host Service**: Aspire Host (optional, mainly for development)

### MCP Gateway Features

The MCP Gateway is a production-optimized container that provides:
- Full MCP protocol compliance
- High availability with autoscaling
- Enhanced security with non-root user
- Protocol testing and validation endpoints
- Comprehensive health checks

## Quick Start

### 1. Deploy MCP Gateway with ArgoCD (Recommended)

> 📖 **Comprehensive ArgoCD Guide**: For detailed ArgoCD deployment automation, including application manifest creation, sync policies configuration, and status verification, see the [**ArgoCD Deployment Guide**](argocd-deployment.md).

#### Prerequisites for Automatic Updates
Ensure ArgoCD Image Updater is installed in your cluster:
```bash
kubectl apply -n argocd -f https://raw.githubusercontent.com/argoproj-labs/argocd-image-updater/stable/manifests/install.yaml
```

#### Deployment Steps

1. Apply the ArgoCD AppProject (optional):
```bash
kubectl apply -f argocd/appproject.yaml
```

2. Deploy the MCP Gateway with automatic image updates:
```bash
kubectl apply -f argocd/application-gateway.yaml
```

3. Deploy the full application (includes API and BFF):
```bash
kubectl apply -f argocd/application.yaml
```

#### Automatic Updates
The ArgoCD applications are configured with argocd-image-updater annotations that will:
- ✅ Monitor GitHub Container Registry for new image tags
- ✅ Automatically update deployments when new `main`, `develop`, or `v*` tags are pushed
- ✅ Update Helm values files via Git commits
- ✅ Trigger ArgoCD sync automatically

#### Monitoring Updates
Check image updater status:
```bash
kubectl logs -n argocd -l app.kubernetes.io/name=argocd-image-updater -f
```

### 2. Manual Deployment with Helm

#### Deploy MCP Gateway Only

```bash
helm install mcp-gateway ./helm/mcp-server-dotnet \
  --namespace mcp-gateway \
  --create-namespace \
  --values helm/mcp-server-dotnet/values-gateway.yaml \
  --set global.tag=main
```

#### Deploy Full Application

```bash
helm install mcp-server ./helm/mcp-server-dotnet \
  --namespace mcp-server \
  --create-namespace \
  --set global.tag=main
```

### 3. Testing the Deployment

Before deploying to production, validate your setup using the provided test script:

```bash
# Run comprehensive deployment tests
./scripts/test-deployment.sh
```

This script will:
- ✅ Validate .NET application builds
- ✅ Run MCP Protocol compliance tests
- ✅ Build and validate Docker containers
- ✅ Check Helm chart configurations
- ✅ Verify ArgoCD configurations
- ✅ Confirm GitHub Actions workflow setup

#### Manual Validation Steps

1. **Check pod status**:
```bash
kubectl get pods -n mcp-gateway
```

2. **View logs**:
```bash
kubectl logs -f deployment/mcp-gateway-service -n mcp-gateway
```

3. **Test health endpoints**:
```bash
kubectl port-forward svc/mcp-gateway-service 8080:80 -n mcp-gateway
curl http://localhost:8080/health
```

4. **Test MCP protocol endpoints**:
```bash
curl http://localhost:8080/api/mcp/tools
curl http://localhost:8080/api/mcp/resources
```

## Configuration

### Environment-specific Values

Create environment-specific values files:

#### Production (`values-prod.yaml`)
```yaml
global:
  tag: "v1.0.0"

api:
  replicaCount: 3
  resources:
    requests:
      cpu: 500m
      memory: 512Mi
    limits:
      cpu: 1000m
      memory: 1Gi

bff:
  replicaCount: 3
  resources:
    requests:
      cpu: 500m
      memory: 512Mi
    limits:
      cpu: 1000m
      memory: 1Gi

ingress:
  hosts:
    - host: mcp-server.yourdomain.com
      paths:
        - path: /api
          pathType: Prefix
          backend:
            service: api
        - path: /
          pathType: Prefix
          backend:
            service: bff
  tls:
    - secretName: mcp-server-tls
      hosts:
        - mcp-server.yourdomain.com

autoscaling:
  enabled: true
  minReplicas: 2
  maxReplicas: 10
```

#### Development (`values-dev.yaml`)
```yaml
global:
  tag: "develop"

api:
  replicaCount: 1
  resources:
    requests:
      cpu: 100m
      memory: 128Mi

bff:
  replicaCount: 1
  resources:
    requests:
      cpu: 100m
      memory: 128Mi

ingress:
  hosts:
    - host: mcp-server-dev.yourdomain.com
```

### Custom Configuration

#### Environment Variables
```yaml
config:
  env:
    CUSTOM_SETTING: "value"
    ANOTHER_SETTING: "another-value"
```

#### Secrets
```yaml
secrets:
  env:
    DATABASE_PASSWORD: "your-password"
    API_KEY: "your-api-key"
```

## CI/CD Pipeline

The GitHub Actions workflow (`.github/workflows/docker-build.yml`) automatically:

1. Builds Docker images for API, BFF, and Host services
2. Pushes images to GitHub Container Registry
3. Tags images appropriately based on branch/tag

### Triggering Deployments

- **Main branch**: Deploys to production with `main` tag
- **Develop branch**: Deploys to development with `develop` tag
- **Tags (v*)**: Creates versioned releases

## ArgoCD Configuration

### Application Structure

The ArgoCD application (`argocd/application.yaml`) is configured with:

- **Auto-sync**: Enabled with prune and self-heal
- **Source**: GitHub repository
- **Destination**: `mcp-server` namespace
- **Sync Policy**: Automated with retry logic

### GitOps Workflow

1. Update Helm values or application code
2. Commit changes to the repository
3. ArgoCD automatically detects changes
4. Application is synced to the cluster

## Monitoring and Observability

### Health Checks

Both API and BFF services include:

- **Liveness Probe**: `/health` endpoint
- **Readiness Probe**: `/health/ready` endpoint

### Metrics (Optional)

Enable Prometheus monitoring:

```yaml
monitoring:
  enabled: true
  serviceMonitor:
    enabled: true
```

## Security Considerations

### Pod Security

The deployment includes:

- Non-root user execution
- Read-only root filesystem
- Dropped capabilities
- Security contexts

### Network Policies

Enable network policies for better security:

```yaml
networkPolicy:
  enabled: true
  ingress:
    - from:
      - namespaceSelector:
          matchLabels:
            name: ingress-nginx
  egress:
    - to: []
```

## Troubleshooting

### Common Issues

1. **Image Pull Errors**
   ```bash
   # Check if images exist in registry
   docker pull ghcr.io/stig-johnny/mcp-server-dotnet/mcp-server-api:main
   
   # Verify image pull secrets
   kubectl get secrets -n mcp-server
   ```

2. **Pod Startup Issues**
   ```bash
   # Check pod logs
   kubectl logs -n mcp-server deployment/mcp-server-dotnet-api
   kubectl logs -n mcp-server deployment/mcp-server-dotnet-bff
   
   # Check pod events
   kubectl describe pod -n mcp-server -l app.kubernetes.io/component=api
   ```

3. **Service Discovery Issues**
   ```bash
   # Test service connectivity
   kubectl exec -it -n mcp-server deployment/mcp-server-dotnet-bff -- curl http://mcp-server-dotnet-api/health
   ```

4. **Ingress Issues**
   ```bash
   # Check ingress status
   kubectl get ingress -n mcp-server
   kubectl describe ingress -n mcp-server mcp-server-dotnet
   ```

### Debugging Commands

```bash
# Check all resources
kubectl get all -n mcp-server

# Check ArgoCD application status
kubectl get application -n argocd mcp-server-dotnet

# View ArgoCD application details
kubectl describe application -n argocd mcp-server-dotnet

# Check Helm release
helm list -n mcp-server
helm status mcp-server -n mcp-server
```

## Rollback Procedures

### ArgoCD Rollback

1. **Via ArgoCD UI**: Navigate to the application and select "History & Rollback"

2. **Via CLI**:
   ```bash
   # Get revision history
   argocd app history mcp-server-dotnet
   
   # Rollback to specific revision
   argocd app rollback mcp-server-dotnet <revision-id>
   ```

### Helm Rollback

```bash
# Check release history
helm history mcp-server -n mcp-server

# Rollback to previous version
helm rollback mcp-server -n mcp-server

# Rollback to specific revision
helm rollback mcp-server 2 -n mcp-server
```

### Emergency Procedures

1. **Scale down problematic service**:
   ```bash
   kubectl scale deployment mcp-server-dotnet-api --replicas=0 -n mcp-server
   ```

2. **Temporarily disable ArgoCD sync**:
   ```bash
   kubectl patch application mcp-server-dotnet -n argocd -p '{"spec":{"syncPolicy":null}}' --type merge
   ```

3. **Quick fix deployment**:
   ```bash
   kubectl set image deployment/mcp-server-dotnet-api api=ghcr.io/stig-johnny/mcp-server-dotnet/mcp-server-api:previous-tag -n mcp-server
   ```

## Production Checklist

- [ ] Update ingress host to production domain
- [ ] Configure TLS certificates
- [ ] Set appropriate resource limits
- [ ] Enable horizontal pod autoscaling
- [ ] Configure monitoring and alerting
- [ ] Set up backup procedures
- [ ] Review security policies
- [ ] Test rollback procedures
- [ ] Document runbooks

## Support

For issues and questions:

1. Check the [GitHub Issues](https://github.com/Stig-Johnny/mcp-server-dotnet/issues)
2. Review ArgoCD and Kubernetes logs
3. Consult the troubleshooting section above