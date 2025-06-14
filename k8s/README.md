# Kubernetes Manifests for MCP Server .NET

This directory contains Kubernetes manifests for deploying GitHub Actions self-hosted runners and related infrastructure for the MCP Server .NET project.

## Directory Structure

```
k8s/
├── github-runners/           # GitHub Actions runners configuration
│   ├── namespace.yaml       # Namespace definition
│   ├── configmap.yaml       # Runner configuration
│   ├── rbac.yaml           # Service account and permissions
│   ├── runner-deployment.yaml # RunnerDeployment resource
│   ├── values.yaml         # Helm-style values file
│   ├── kustomization.yaml  # Kustomize configuration
│   └── patches/            # Environment-specific patches
│       ├── development.yaml # Development environment overrides
│       └── production.yaml  # Production environment overrides
└── monitoring/              # Monitoring and observability
    ├── service-monitor.yaml # Prometheus ServiceMonitor
    └── grafana-dashboard.yaml # Grafana dashboard ConfigMap
```

## Quick Start

### Prerequisites

1. **GitHub Actions Runner Controller** must be installed in your cluster:
   ```bash
   kubectl apply -f https://github.com/actions/actions-runner-controller/releases/latest/download/actions-runner-controller.yaml
   ```

2. **GitHub Personal Access Token (PAT)** with required permissions:
   - `repo` (Full control of private repositories)
   - `workflow` (Update GitHub Action workflows)
   - `admin:repo_hook` (Full control of repository hooks)

3. **Create Kubernetes Secret** for the GitHub PAT:
   ```bash
   kubectl create secret generic github-actions-secret \
     --namespace=github-runners \
     --from-literal=GITHUB_TOKEN=<your-github-pat>
   ```

### Deployment Options

#### Option 1: Using ArgoCD (Recommended)

Deploy using the provided ArgoCD applications:

```bash
# Development environment
kubectl apply -f ../argocd/runners-application-dev.yaml

# Production environment  
kubectl apply -f ../argocd/runners-application-prod.yaml
```

#### Option 2: Direct Kubectl

Deploy directly to Kubernetes:

```bash
# Apply all manifests
kubectl apply -f github-runners/
kubectl apply -f monitoring/

# Or use Kustomize
kubectl apply -k github-runners/
```

#### Option 3: Environment-Specific Deployment

Deploy with environment-specific configurations:

```bash
# Development
cd github-runners
kubectl kustomize . | kubectl apply -f -

# Production (with patches)
cd github-runners
kubectl kustomize . --load-restrictor=LoadRestrictionsNone | kubectl apply -f -
```

## Configuration

### Runner Configuration

The runners are configured via `github-runners/configmap.yaml`:

| Setting | Default | Description |
|---------|---------|-------------|
| `RUNNER_SCOPE` | `repo` | Scope of the runner (repo/org) |
| `RUNNER_LABELS` | `self-hosted,kubernetes,mcp-server-dotnet` | Labels for workflow targeting |
| `RUNNER_GROUP` | `mcp-server-runners` | Runner group name |
| `DOCKER_ENABLED` | `true` | Enable Docker in runners |
| `EPHEMERAL` | `false` | Use ephemeral runners |
| `LOG_LEVEL` | `info` | Logging level |

### Resource Configuration

Default resource limits in `runner-deployment.yaml`:

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

Adjust the number of runners:

```yaml
spec:
  replicas: 2  # Change this value
```

Or scale dynamically:

```bash
kubectl scale runnerdeployment mcp-server-dotnet-runners -n github-runners --replicas=5
```

## Environment-Specific Configurations

### Development Environment

- **Namespace**: `github-runners-dev`
- **Replicas**: 1
- **Resources**: Reduced (1 CPU, 2Gi memory)
- **Source**: `develop` branch
- **Labels**: Includes `development` label

### Production Environment

- **Namespace**: `github-runners`
- **Replicas**: 3
- **Resources**: Enhanced (4 CPU, 8Gi memory)
- **Source**: `main` branch
- **Labels**: Includes `production` label
- **Node Selection**: Dedicated runner nodes
- **Tolerations**: For dedicated node scheduling

## Monitoring and Observability

### Prometheus Metrics

The ServiceMonitor (`monitoring/service-monitor.yaml`) collects:
- Runner status and availability
- Job execution metrics
- Resource utilization
- Queue metrics

### Grafana Dashboard

Import the dashboard from `monitoring/grafana-dashboard.yaml`:
- Active runners count
- Job execution rate
- Resource usage trends
- Recent workflow runs

### Logs

View runner logs:

```bash
# All runners
kubectl logs -l app.kubernetes.io/name=mcp-server-dotnet-runners -n github-runners -f

# Specific runner
kubectl logs <runner-pod-name> -n github-runners -f
```

## Security

### RBAC

The configuration includes minimal RBAC permissions:
- Read access to secrets and configmaps
- Read access to pods (for debugging)
- Service account with restricted permissions

### Security Context

Runners run with security context:
- Non-root user (UID 1000)
- Non-root group (GID 1000)
- Read-only root filesystem where possible

### Network Security

- Isolated namespace
- Network policies can be applied
- Minimal exposed ports

## Troubleshooting

### Common Issues

#### Runners Not Connecting

1. **Check GitHub PAT**:
   ```bash
   kubectl get secret github-actions-secret -n github-runners -o yaml
   ```

2. **Verify secret format**:
   ```bash
   kubectl get secret github-actions-secret -n github-runners -o jsonpath='{.data.GITHUB_TOKEN}' | base64 -d
   ```

#### Pods Not Starting

1. **Check pod status**:
   ```bash
   kubectl get pods -n github-runners
   kubectl describe pod <pod-name> -n github-runners
   ```

2. **Check events**:
   ```bash
   kubectl get events -n github-runners --sort-by='.firstTimestamp'
   ```

#### Resource Issues

1. **Check resource usage**:
   ```bash
   kubectl top pods -n github-runners
   kubectl top nodes
   ```

2. **Check resource constraints**:
   ```bash
   kubectl describe pod <pod-name> -n github-runners | grep -A 10 Resources
   ```

### Debug Commands

```bash
# Get all resources
kubectl get all -n github-runners

# Check RunnerDeployment status
kubectl get runnerdeployment -n github-runners

# Check individual runners
kubectl get runner -n github-runners

# Check controller logs
kubectl logs -n actions-runner-system -l app.kubernetes.io/name=actions-runner-controller -f
```

## Customization

### Adding Custom Labels

Edit `configmap.yaml`:

```yaml
data:
  RUNNER_LABELS: "self-hosted,kubernetes,mcp-server-dotnet,custom-label"
```

### Custom Resource Limits

Edit `runner-deployment.yaml`:

```yaml
resources:
  limits:
    cpu: "4"
    memory: 8Gi
```

### Node Selection

Add node selector to `runner-deployment.yaml`:

```yaml
nodeSelector:
  kubernetes.io/arch: amd64
  node-type: github-runners
```

### Additional Environment Variables

Add to `runner-deployment.yaml`:

```yaml
env:
- name: CUSTOM_VAR
  value: "custom-value"
```

## Maintenance

### Updating Runners

Update the runner image:

```bash
kubectl patch runnerdeployment mcp-server-dotnet-runners -n github-runners \
  --type='merge' -p='{"spec":{"template":{"spec":{"image":"summerwind/actions-runner:latest"}}}}'
```

### Backup Configuration

```bash
# Backup all runner resources
kubectl get runnerdeployment,configmap,secret,serviceaccount,role,rolebinding \
  -n github-runners -o yaml > runner-backup.yaml
```

### Cleanup

```bash
# Remove all resources
kubectl delete -f github-runners/
kubectl delete -f monitoring/

# Or using Kustomize
kubectl delete -k github-runners/
```

## Integration with Existing Infrastructure

This configuration integrates with the existing MCP Server .NET infrastructure:

- **ArgoCD**: Uses existing ArgoCD setup for GitOps
- **Monitoring**: Integrates with existing Prometheus/Grafana stack
- **Secrets**: Uses Kubernetes native secret management
- **Networking**: Compatible with existing network policies
- **RBAC**: Follows existing security patterns

## Support

For issues and questions:

1. Check the [main documentation](../docs/github-runners-setup.md)
2. Review [GitHub Issues](https://github.com/Stig-Johnny/mcp-server-dotnet/issues)
3. Check runner controller [documentation](https://github.com/actions/actions-runner-controller)
4. Review Kubernetes cluster logs and events