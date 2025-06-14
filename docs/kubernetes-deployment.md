# MCP Server .NET - Kubernetes Deployment Guide

This guide provides instructions for deploying the MCP Server .NET Aspire application to a Kubernetes cluster using ArgoCD and Helm.

## Prerequisites

- Kubernetes cluster (1.19+)
- ArgoCD installed in the cluster
- Helm 3.x
- kubectl configured to access your cluster
- Container registry access (GitHub Container Registry is used by default)

## Architecture Overview

The deployment consists of:

- **API Service**: MCP Server API (`McpServer.Api`)
- **BFF Service**: Backend-for-Frontend with React frontend (`McpServer.Bff`)
- **Host Service**: Aspire Host (optional, mainly for development)

## Quick Start

### 1. Deploy with ArgoCD

1. Apply the ArgoCD AppProject (optional):
```bash
kubectl apply -f argocd/appproject.yaml
```

2. Deploy the application:
```bash
kubectl apply -f argocd/application.yaml
```

### 2. Manual Deployment with Helm

1. Add the repository (if using a Helm repository):
```bash
helm repo add mcp-server https://your-helm-repo.com
helm repo update
```

2. Install the chart:
```bash
helm install mcp-server ./helm/mcp-server-dotnet \
  --namespace mcp-server \
  --create-namespace \
  --set global.tag=main
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