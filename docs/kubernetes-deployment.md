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

4. Deploy staging environment (for validation):
```bash
kubectl apply -f argocd/application-staging.yaml
kubectl apply -f argocd/application-gateway-staging.yaml
```

5. Deploy development environment (for testing):
```bash
kubectl apply -f argocd/application-dev.yaml
kubectl apply -f argocd/application-gateway-dev.yaml
```

#### Automatic Updates
The ArgoCD applications are configured with argocd-image-updater annotations that will:
- ✅ Monitor GitHub Container Registry for new image tags
- ✅ Automatically update deployments when new `main` or `v*` tags are pushed
- ✅ Update Helm values files via Git commits
- ✅ Trigger ArgoCD sync automatically
- ✅ Follow GitOps promotion workflow (staging → production)

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

#### Service Access

**Important**: As of the latest version, ingress resources with custom domains have been removed from the Helm charts and are expected to be managed by external software (external ingress controllers, service mesh, or load balancers).

The application services are accessible through:

##### Internal Kubernetes Service URLs

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

> **💡 Service Access Options**: Access services via:
> - **Port forwarding**: `kubectl port-forward svc/service-name 8080:80`
> - **External ingress controllers**: Configure your external ingress to route to these services
> - **Service mesh**: Use Istio, Linkerd, or other service mesh for routing
> - **Load balancers**: Configure external load balancers to target these services
> - **Custom ingress**: Enable ingress in values files and configure without custom domains

##### Access Methods

1. **Port Forwarding (Development)**:
   ```bash
   # Access Gateway service
   kubectl port-forward svc/mcp-server-dotnet-gateway 8080:80 -n mcp-server
   
   # Access API service  
   kubectl port-forward svc/mcp-server-dotnet-api 8080:80 -n mcp-server
   
   # Access BFF service
   kubectl port-forward svc/mcp-server-dotnet-bff 8080:80 -n mcp-server
   ```

2. **External Ingress Management**:
   Configure your external ingress controller to route traffic to the internal service endpoints listed above.

3. **Custom Ingress Configuration**:
   If you need to enable ingress within the Helm chart (without custom domains), set:
   ```yaml
   ingress:
     enabled: true
     # Configure hosts and paths as needed for your environment
     # without using custom domains like *.yourdomain.com
   ```

##### Option 2: Cloudflare Tunnels

Cloudflare Tunnels provide secure access without exposing your cluster directly to the internet.

##### Option 2a: GitOps Deployment (Recommended)

The MCP Server includes integrated Cloudflare Tunnel support via GitOps deployment using Helm charts and ArgoCD.

1. **Create Cloudflare Tunnel**:
   ```bash
   # Create tunnel for your domain
   cloudflared tunnel create mcp-server-prod
   cloudflared tunnel create mcp-server-staging
   cloudflared tunnel create mcp-server-dev
   
   # Note the tunnel IDs from the output
   ```

2. **Configure DNS Records**:
   ```bash
   # Add CNAME records for production
   cloudflared tunnel route dns mcp-server-prod mcp-server.stigjohnny.no
   cloudflared tunnel route dns mcp-server-prod mcp-gateway.stigjohnny.no
   cloudflared tunnel route dns mcp-server-prod mcp-api.stigjohnny.no
   
   # Add CNAME records for staging
   cloudflared tunnel route dns mcp-server-staging mcp-staging.stigjohnny.no
   cloudflared tunnel route dns mcp-server-staging mcp-gateway-staging.stigjohnny.no
   cloudflared tunnel route dns mcp-server-staging mcp-api-staging.stigjohnny.no
   
   # Add CNAME records for development
   cloudflared tunnel route dns mcp-server-dev mcp-dev.stigjohnny.no
   cloudflared tunnel route dns mcp-server-dev mcp-gateway-dev.stigjohnny.no
   cloudflared tunnel route dns mcp-server-dev mcp-api-dev.stigjohnny.no
   ```

3. **Create Kubernetes Secrets** (manual step required):
   ```bash
   # Production environment
   kubectl create namespace mcp-server
   kubectl create secret generic mcp-server-dotnet-cloudflared-creds \
     --from-file=credentials.json=~/.cloudflared/<tunnel-id>.json \
     --namespace=mcp-server
   
   # Repeat for staging and development environments as needed
   ```

4. **Update Helm Values** with actual tunnel IDs:
   ```bash
   # Edit values files to replace REPLACE_WITH_ACTUAL_TUNNEL_ID
   # Production: helm/mcp-server-dotnet/values-prod.yaml
   # Staging: helm/mcp-server-dotnet/values-staging.yaml
   # Development: helm/mcp-server-dotnet/values-dev.yaml
   ```

5. **Deploy via ArgoCD**:
   ```bash
   # ArgoCD will automatically deploy cloudflared along with the applications
   kubectl apply -f argocd/app-of-apps.yaml
   ```

The GitOps approach provides:
- ✅ Integrated deployment with application lifecycle
- ✅ Environment-specific tunnel configurations
- ✅ Automatic scaling and pod disruption budgets
- ✅ Consistent resource management
- ✅ Configuration drift detection and remediation

**Configured Domains for stigjohnny.no**:
- **Production**: `mcp-server.stigjohnny.no`, `mcp-gateway.stigjohnny.no`, `mcp-api.stigjohnny.no`
- **Staging**: `mcp-staging.stigjohnny.no`, `mcp-gateway-staging.stigjohnny.no`, `mcp-api-staging.stigjohnny.no`
- **Development**: `mcp-dev.stigjohnny.no`, `mcp-gateway-dev.stigjohnny.no`, `mcp-api-dev.stigjohnny.no`

##### Option 2b: Manual Deployment (Legacy)

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

After setting up external ingress management (e.g., Cloudflare Tunnels), verify your configuration:

```bash
# Test service accessibility via port forwarding (for internal testing)
kubectl port-forward svc/mcp-server-dotnet-gateway 8080:80 -n mcp-server
curl http://localhost:8080/health

# Test external access (if using Cloudflare Tunnels or external ingress)
curl -k https://your-configured-domain.com/health

# Verify MCP protocol endpoints
curl -k https://your-configured-domain.com/api/mcp/tools

# Check internal service connectivity
kubectl exec -n mcp-server deployment/mcp-server-dotnet-api -- curl http://mcp-server-dotnet-gateway:80/health
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

# Note: Ingress with custom domains is disabled - use external ingress management
ingress:
  enabled: false

autoscaling:
  enabled: true
  minReplicas: 2
  maxReplicas: 10
```

#### Development (`values-dev.yaml`)
```yaml
global:
  tag: "main"

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

# Note: Ingress with custom domains is disabled - use external ingress management
ingress:
  enabled: false
```

#### Staging (`values-staging.yaml`)
```yaml
global:
  tag: "main"

api:
  replicaCount: 2
  resources:
    requests:
      cpu: 200m
      memory: 256Mi

bff:
  replicaCount: 2
  resources:
    requests:
      cpu: 200m
      memory: 256Mi

autoscaling:
  enabled: true
  minReplicas: 2
  maxReplicas: 4

podDisruptionBudget:
  enabled: true
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

- **Main branch**: Triggers GitOps promotion workflow (staging → production) with `main` tag
- **Tags (v*)**: Creates versioned releases

### GitOps Promotion Workflow

The deployment follows a GitOps promotion pattern:

1. **Main Branch Push** → Triggers CI/CD pipeline
2. **Staging Deployment** → Automatic deployment to staging environment
3. **Production Deployment** → Automatic promotion to production after staging validation

## ArgoCD Configuration

### Application Structure

The ArgoCD application (`argocd/application.yaml`) is configured with:

- **Auto-sync**: Enabled with prune and self-heal
- **Source**: GitHub repository
- **Destination**: `mcp-server` namespace
- **Sync Policy**: Automated with retry logic

### GitOps Workflow

1. Update Helm values or application code
2. Commit changes to the main branch
3. ArgoCD automatically detects changes
4. Applications are synced to environments in sequence:
   - Development environment (continuous deployment)
   - Staging environment (validation)
   - Production environment (after staging validation)

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