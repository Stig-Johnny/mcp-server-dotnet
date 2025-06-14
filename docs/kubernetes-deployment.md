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

### Hostname Configuration

The MCP Server .NET application uses different hostnames for different deployment scenarios. Understanding these hostnames is crucial for proper DNS setup and ingress configuration.

#### Components and Architecture

The MCP Server .NET deployment includes the following components:

**Complete Stack (MVP Server)**:
- **MCP Gateway**: Protocol-compliant MCP server with enhanced features
- **API Service**: Core MCP Server API (`McpServer.Api`)
- **BFF Service**: Backend-for-Frontend with React application (`McpServer.Bff`)

**Gateway-Only Deployment**:
- **MCP Gateway**: Standalone protocol-compliant server for production MCP operations

#### Internal Kubernetes Service URLs

When deployed with the default setup, the following internal Kubernetes service names are created:

**Full Stack Deployment** (namespace: `mcp-server`):
```
mcp-server-dotnet-gateway.mcp-server.svc.cluster.local:80
mcp-server-dotnet-api.mcp-server.svc.cluster.local:80
mcp-server-dotnet-bff.mcp-server.svc.cluster.local:80
```

**Gateway-Only Deployment** (namespace: `mcp-gateway`):
```
mcp-gateway-gateway.mcp-gateway.svc.cluster.local:80
```

> **💡 Internal Routing**: Use these service URLs for:
> - Custom Ingress controller configurations
> - Service mesh routing (Istio, Linkerd)
> - Internal service-to-service communication
> - Load balancer backend pool configurations

#### Hostname Overview

| Environment | Hostname | Purpose | Routes |
|-------------|----------|---------|--------|
| **Development** | `mcp-server-dev.yourdomain.com` | Development testing and staging | `/api` → API service<br>`/` → BFF service |
| **Production** | `mcp-server.yourdomain.com` | Production deployment | `/api` → API service<br>`/` → BFF service |
| **Gateway** | `mcp-gateway.local` | MCP protocol compliance | `/api/mcp` → Gateway service<br>`/health` → Gateway service<br>`/swagger` → Gateway service<br>`/api` → API service |

#### Hostname Purposes

- **Development/Production hostnames** (`mcp-server-dev.yourdomain.com`, `mcp-server.yourdomain.com`):
  - Full application deployment with both API and BFF (Backend-for-Frontend) services
  - BFF serves the React frontend application
  - API provides the core MCP server functionality
  - Suitable for complete application testing and production use

- **Gateway hostname** (`mcp-gateway.local`):
  - Focused on MCP protocol compliance
  - Optimized for production MCP protocol operations
  - Direct access to MCP endpoints without frontend overhead
  - Includes health checks and API documentation routes

#### Setting Up Hostnames

##### Option 1: Kubernetes Ingress with DNS

1. **Configure your DNS provider** to point hostnames to your Kubernetes cluster's ingress controller:
   ```bash
   # Example DNS A records
   mcp-server.yourdomain.com        → <ingress-controller-ip>
   mcp-server-dev.yourdomain.com    → <ingress-controller-ip>
   mcp-gateway.local                → <ingress-controller-ip>
   ```

2. **Update Helm values** with your actual domain:
   ```yaml
   # values-prod.yaml
   ingress:
     hosts:
       - host: mcp-server.yourdomain.com  # Replace with your domain
   
   # values-dev.yaml
   ingress:
     hosts:  
       - host: mcp-server-dev.yourdomain.com  # Replace with your domain
   
   # values-gateway.yaml
   ingress:
     hosts:
       - host: mcp-gateway.yourdomain.com  # Replace with your domain if needed
   ```

3. **Configure TLS certificates** (recommended for production):
   ```yaml
   ingress:
     tls:
       - secretName: mcp-server-tls
         hosts:
           - mcp-server.yourdomain.com
   ```

##### Option 2: Cloudflare Tunnels

Cloudflare Tunnels provide secure access without exposing your cluster directly to the internet.

1. **Install cloudflared** in your cluster:
   ```bash
   # Create tunnel
   cloudflared tunnel create mcp-server
   
   # Generate configuration
   cat > config.yaml << EOF
   tunnel: <tunnel-id>
   credentials-file: /etc/cloudflared/creds/<tunnel-id>.json
   
   ingress:
     - hostname: mcp-server.yourdomain.com
       service: http://mcp-server-dotnet-bff.mcp-server.svc.cluster.local:80
     - hostname: mcp-server-dev.yourdomain.com  
       service: http://mcp-server-dotnet-bff.mcp-server.svc.cluster.local:80
     - hostname: mcp-gateway.yourdomain.com
       service: http://mcp-gateway-gateway.mcp-gateway.svc.cluster.local:80
     - service: http_status:404
   EOF
   ```

2. **Deploy Cloudflare tunnel** to Kubernetes:
   ```bash
   kubectl create secret generic tunnel-credentials \
     --from-file=credentials.json=/path/to/<tunnel-id>.json \
     --namespace=mcp-server
   
   kubectl create configmap tunnel-config \
     --from-file=config.yaml=config.yaml \
     --namespace=mcp-server
   ```

3. **Create tunnel deployment**:
   ```yaml
   apiVersion: apps/v1
   kind: Deployment
   metadata:
     name: cloudflared
     namespace: mcp-server
   spec:
     replicas: 2
     selector:
       matchLabels:
         app: cloudflared
     template:
       metadata:
         labels:
           app: cloudflared
       spec:
         containers:
         - name: cloudflared
           image: cloudflare/cloudflared:latest
           args:
           - tunnel
           - --config
           - /etc/cloudflared/config/config.yaml
           - run
           volumeMounts:
           - name: config
             mountPath: /etc/cloudflared/config
           - name: creds
             mountPath: /etc/cloudflared/creds
         volumes:
         - name: config
           configMap:
             name: tunnel-config
         - name: creds
           secret:
             secretName: tunnel-credentials
   ```

##### Option 3: Local Development (Port Forwarding)

For local testing without DNS setup:

```bash
# Forward services to localhost
kubectl port-forward svc/mcp-server-dotnet-bff 8080:80 -n mcp-server
kubectl port-forward svc/mcp-gateway-gateway 8081:80 -n mcp-gateway

# Access via localhost
curl http://localhost:8080/api/mcp/tools
curl http://localhost:8081/api/mcp/tools
```

#### Verification

After setting up hostnames, verify your configuration:

```bash
# Test hostname resolution
nslookup mcp-server.yourdomain.com
nslookup mcp-server-dev.yourdomain.com

# Test HTTP endpoints
curl -k https://mcp-server.yourdomain.com/health
curl -k https://mcp-gateway.yourdomain.com/health

# Verify MCP protocol endpoints
curl -k https://mcp-gateway.yourdomain.com/api/mcp/tools
```

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

- [ ] Configure hostnames and DNS (see [Hostname Configuration](#hostname-configuration))
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