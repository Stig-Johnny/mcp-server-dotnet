# GitHub Actions Self-Hosted Runners Setup Guide

This guide provides comprehensive instructions for setting up dedicated GitHub Actions self-hosted runners in a Kubernetes cluster for the `Stig-Johnny/mcp-server-dotnet` repository.

## Overview

The self-hosted runners setup includes:
- **GitHub Actions Runner Controller** for managing runners
- **Kubernetes manifests** for runner deployment
- **GitOps integration** with ArgoCD
- **Monitoring and observability** with Prometheus and Grafana
- **Secure secrets management** with Kubernetes secrets

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Kubernetes Cluster                       │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │ GitHub Runners  │  │   Monitoring    │  │     ArgoCD      │ │
│  │   Namespace     │  │   Namespace     │  │   Namespace     │ │
│  │                 │  │                 │  │                 │ │
│  │ • RunnerDeploy  │  │ • ServiceMonitor│  │ • Application   │ │
│  │ • ConfigMap     │  │ • Grafana Dashboard│  │ • Sync Policy  │ │
│  │ • RBAC          │  │ • Prometheus    │  │ • Auto-sync     │ │
│  │ • Secrets       │  │   Rules         │  │                 │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
                  ┌─────────────────────────┐
                  │     GitHub Actions      │
                  │     mcp-server-dotnet   │
                  │       Repository        │
                  └─────────────────────────┘
```

## Prerequisites

Before setting up the runners, ensure you have:

1. **Kubernetes cluster** (1.19+) with cluster admin access
2. **GitHub Actions Runner Controller** installed
3. **ArgoCD** installed and configured
4. **Prometheus Operator** (optional, for monitoring)
5. **kubectl** configured to access your cluster
6. **Helm 3.x** (for related deployments)

## Installation Steps

### 1. Install GitHub Actions Runner Controller

First, install the GitHub Actions Runner Controller in your cluster:

```bash
# Install GitHub Actions Runner Controller
kubectl apply -f https://github.com/actions/actions-runner-controller/releases/latest/download/actions-runner-controller.yaml

# Verify installation
kubectl get pods -n actions-runner-system
```

### 2. Create GitHub Personal Access Token (PAT)

Create a GitHub PAT with the following permissions:
- `repo` (Full control of private repositories)
- `workflow` (Update GitHub Action workflows)
- `admin:repo_hook` (Full control of repository hooks)

### 3. Create Kubernetes Secret

Create a secret to store the GitHub PAT:

```bash
kubectl create secret generic github-actions-secret \
  --namespace=github-runners \
  --from-literal=GITHUB_TOKEN=<your-github-pat>
```

### 4. Deploy using ArgoCD (Recommended)

Deploy the runners using the provided ArgoCD application:

```bash
# Apply the ArgoCD application
kubectl apply -f argocd/runners-application.yaml

# Check application status
kubectl get application github-runners -n argocd
```

### 5. Manual Deployment (Alternative)

If not using ArgoCD, deploy manually:

```bash
# Create namespace and resources
kubectl apply -f k8s/github-runners/
kubectl apply -f k8s/monitoring/

# Verify deployment
kubectl get pods -n github-runners
```

## Configuration

### Runner Configuration

The runners are configured via ConfigMap (`k8s/github-runners/configmap.yaml`):

```yaml
# Key configuration options
RUNNER_SCOPE: "repo"                    # Scope to repository
RUNNER_LABELS: "self-hosted,kubernetes,mcp-server-dotnet"  # Runner labels
RUNNER_GROUP: "mcp-server-runners"      # Runner group
DOCKER_ENABLED: "true"                  # Enable Docker
EPHEMERAL: "false"                      # Persistent runners
```

### Resource Limits

Adjust resource limits in `runner-deployment.yaml`:

```yaml
resources:
  requests:
    cpu: 100m
    memory: 128Mi
  limits:
    cpu: "2"
    memory: 4Gi
```

### Scaling

Scale the runners by modifying the `replicas` field:

```yaml
spec:
  replicas: 3  # Adjust based on your needs
```

## Monitoring and Observability

### Prometheus Metrics

The setup includes ServiceMonitor for Prometheus:
- Runner status and availability
- Job execution metrics
- Resource utilization
- Queue length and wait times

### Grafana Dashboard

Import the provided Grafana dashboard:

```bash
# The dashboard is automatically deployed via ConfigMap
kubectl get configmap github-runners-grafana-dashboard -n github-runners
```

### Logs

View runner logs:

```bash
# View all runner pods
kubectl get pods -n github-runners

# View logs for a specific runner
kubectl logs <runner-pod-name> -n github-runners -f

# View logs for all runners
kubectl logs -l app.kubernetes.io/name=mcp-server-dotnet-runners -n github-runners -f
```

## Usage in Workflows

### Using Self-Hosted Runners

Target your workflows to use the self-hosted runners:

```yaml
name: Build with Self-Hosted Runners
on: [push, pull_request]

jobs:
  build:
    runs-on: [self-hosted, kubernetes, mcp-server-dotnet]
    steps:
    - uses: actions/checkout@v4
    # ... your build steps
```

### Available Labels

Use these labels to target specific runners:
- `self-hosted` - All self-hosted runners
- `kubernetes` - Kubernetes-based runners
- `mcp-server-dotnet` - Repository-specific runners
- `linux` - Linux runners
- `x64` - x64 architecture

### Example Workflows

See the provided example workflow:
- `.github/workflows/self-hosted-build.yml` - Resource-intensive builds

## Troubleshooting

### Common Issues

#### 1. Runners Not Connecting

Check the GitHub PAT and secret:

```bash
# Verify secret exists
kubectl get secret github-actions-secret -n github-runners

# Check secret content (base64 encoded)
kubectl get secret github-actions-secret -n github-runners -o yaml
```

#### 2. Pods Not Starting

Check pod status and events:

```bash
# Check pod status
kubectl get pods -n github-runners

# Describe problematic pod
kubectl describe pod <pod-name> -n github-runners

# Check events
kubectl get events -n github-runners --sort-by='.firstTimestamp'
```

#### 3. Docker Issues

Verify Docker configuration:

```bash
# Check if Docker is enabled in runner
kubectl logs <runner-pod> -n github-runners | grep -i docker

# Check security context
kubectl describe pod <runner-pod> -n github-runners | grep -i security
```

#### 4. Resource Constraints

Check resource usage:

```bash
# Check resource requests/limits
kubectl describe pod <runner-pod> -n github-runners | grep -A 10 -i resources

# Check node resources
kubectl top nodes
kubectl top pods -n github-runners
```

### Debug Commands

```bash
# Get all resources in namespace
kubectl get all -n github-runners

# Check runner deployment status
kubectl get runnerdeployment -n github-runners

# Check runner set status
kubectl get runnerset -n github-runners

# Check runner status
kubectl get runner -n github-runners

# View ArgoCD application
kubectl get application github-runners -n argocd -o yaml
```

## Security Considerations

### Network Security
- Runners run in isolated namespace
- RBAC limits permissions
- Network policies can be applied

### Container Security
- Non-root user execution
- Read-only root filesystem where possible
- Minimal base images

### Secrets Management
- GitHub PAT stored in Kubernetes secrets
- Secrets mounted as environment variables
- Automatic secret rotation supported

## Maintenance

### Updating Runners

Update runner images:

```bash
# Update runner deployment
kubectl patch runnerdeployment mcp-server-dotnet-runners -n github-runners \
  --type='merge' -p='{"spec":{"template":{"spec":{"image":"<new-image>"}}}}'
```

### Scaling Operations

```bash
# Scale up
kubectl scale runnerdeployment mcp-server-dotnet-runners -n github-runners --replicas=5

# Scale down
kubectl scale runnerdeployment mcp-server-dotnet-runners -n github-runners --replicas=1
```

### Backup and Recovery

```bash
# Backup runner configuration
kubectl get runnerdeployment mcp-server-dotnet-runners -n github-runners -o yaml > runner-backup.yaml

# Backup secrets
kubectl get secret github-actions-secret -n github-runners -o yaml > secret-backup.yaml
```

## Performance Tuning

### Node Selection

Use node selectors for dedicated nodes:

```yaml
nodeSelector:
  kubernetes.io/arch: amd64
  node-type: github-runners
```

### Resource Optimization

Adjust based on workload:

```yaml
# For CPU-intensive workloads
resources:
  limits:
    cpu: "4"
    memory: 8Gi

# For memory-intensive workloads
resources:
  limits:
    cpu: "2"
    memory: 16Gi
```

### Caching Strategies

Enable caching for better performance:

```yaml
# In workflow
- uses: actions/cache@v3
  with:
    path: ~/.nuget/packages
    key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
```

## Support and Contributing

### Getting Help

1. Check the [GitHub Issues](https://github.com/Stig-Johnny/mcp-server-dotnet/issues)
2. Review runner controller [documentation](https://github.com/actions/actions-runner-controller)
3. Check Kubernetes cluster logs
4. Review ArgoCD application status

### Contributing

To contribute improvements:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## Additional Resources

- [GitHub Actions Runner Controller](https://github.com/actions/actions-runner-controller)
- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [ArgoCD Documentation](https://argoproj.github.io/argo-cd/)
- [Prometheus Operator](https://github.com/prometheus-operator/prometheus-operator)
- [Grafana Documentation](https://grafana.com/docs/)