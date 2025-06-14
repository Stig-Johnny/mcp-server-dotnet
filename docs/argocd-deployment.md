# MCP Gateway ArgoCD Deployment Automation Guide

This comprehensive guide provides detailed steps for automating the deployment of MCP Gateway using ArgoCD, including application manifest creation, sync policies configuration, and application status verification. This documentation reflects the enhancements from pull request #7, which added ArgoCD Image Updater integration for automated container image updates.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [ArgoCD Application Architecture](#argocd-application-architecture)
3. [Creating Application Manifests](#creating-application-manifests)
4. [Sync Policies Configuration](#sync-policies-configuration)
5. [ArgoCD Image Updater Integration](#argocd-image-updater-integration)
6. [Deployment Procedures](#deployment-procedures)
7. [Application Status Verification](#application-status-verification)
8. [Troubleshooting](#troubleshooting)
9. [Best Practices](#best-practices)

## Prerequisites

Before deploying MCP Gateway with ArgoCD, ensure you have:

### Required Components
- **Kubernetes cluster** (version 1.19+)
- **ArgoCD** installed and configured in your cluster
- **ArgoCD Image Updater** for automatic image updates
- **Helm 3.x** for chart management
- **kubectl** configured with cluster access
- **Git repository access** (GitHub Container Registry by default)

### Install ArgoCD Image Updater

Install ArgoCD Image Updater to enable automatic Docker image updates:

```bash
# Install ArgoCD Image Updater
kubectl apply -n argocd -f https://raw.githubusercontent.com/argoproj-labs/argocd-image-updater/stable/manifests/install.yaml

# Verify installation
kubectl get pods -n argocd -l app.kubernetes.io/name=argocd-image-updater
```

### Configure Container Registry Access

Create a pull secret for GitHub Container Registry access:

```bash
# Create namespace if it doesn't exist
kubectl create namespace argocd --dry-run=client -o yaml | kubectl apply -f -

# Create pull secret for GitHub Container Registry
kubectl create secret docker-registry ghcr-secret \
  --docker-server=ghcr.io \
  --docker-username=<github-username> \
  --docker-password=<github-token> \
  --namespace=argocd
```

## ArgoCD Application Architecture

The MCP Gateway deployment uses a multi-application ArgoCD architecture designed for scalability and maintainability:

### Application Structure

```
ArgoCD Applications
├── mcp-gateway (Gateway-only deployment)
│   ├── Namespace: mcp-gateway
│   ├── Helm Chart: helm/mcp-server-dotnet
│   ├── Values: values-gateway.yaml
│   └── Image Updater: Enabled
└── mcp-server-dotnet (Full application deployment)
    ├── Namespace: mcp-server
    ├── Helm Chart: helm/mcp-server-dotnet
    ├── Values: values.yaml
    └── Multi-service Image Updater: Enabled
```

### Key Design Principles

1. **Separation of Concerns**: Gateway deployment separate from full application
2. **Automated Updates**: Image Updater integration for zero-touch deployments
3. **Environment Isolation**: Different namespaces for different services
4. **GitOps Workflow**: All changes tracked through Git commits
5. **High Availability**: Built-in retry logic and self-healing capabilities

## Creating Application Manifests

### 1. AppProject Configuration

First, create an AppProject to define security boundaries and resource access:

```yaml
# File: argocd/appproject.yaml
apiVersion: argoproj.io/v1alpha1
kind: AppProject
metadata:
  name: mcp-server
  namespace: argocd
spec:
  description: MCP Server .NET Project
  sourceRepos:
  - 'https://github.com/Stig-Johnny/mcp-server-dotnet.git'
  destinations:
  - namespace: mcp-server
    server: https://kubernetes.default.svc
  - namespace: mcp-server-dev
    server: https://kubernetes.default.svc
  - namespace: mcp-server-staging
    server: https://kubernetes.default.svc
  - namespace: mcp-gateway
    server: https://kubernetes.default.svc
  clusterResourceWhitelist:
  - group: ''
    kind: Namespace
  - group: rbac.authorization.k8s.io
    kind: ClusterRole
  - group: rbac.authorization.k8s.io
    kind: ClusterRoleBinding
  namespaceResourceWhitelist:
  - group: ''
    kind: ConfigMap
  - group: ''
    kind: Secret
  - group: ''
    kind: Service
  - group: ''
    kind: ServiceAccount
  - group: apps
    kind: Deployment
  - group: apps
    kind: ReplicaSet
  - group: networking.k8s.io
    kind: Ingress
  - group: networking.k8s.io
    kind: NetworkPolicy
  - group: policy
    kind: PodDisruptionBudget
  - group: autoscaling
    kind: HorizontalPodAutoscaler
  roles:
  - name: admin
    description: Admin access to mcp-server project
    policies:
    - p, proj:mcp-server:admin, applications, *, mcp-server/*, allow
    - p, proj:mcp-server:admin, repositories, *, *, allow
    groups:
    - argocd-admins
```

**Apply the AppProject:**
```bash
kubectl apply -f argocd/appproject.yaml
```

### 2. MCP Gateway Application Manifest

Create the dedicated Gateway application manifest:

```yaml
# File: argocd/application-gateway.yaml
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: mcp-gateway
  namespace: argocd
  labels:
    app.kubernetes.io/name: mcp-gateway
    app.kubernetes.io/component: gateway
  annotations:
    argocd.argoproj.io/sync-wave: "0"
    # ArgoCD Image Updater configuration for automatic Docker image updates
    argocd-image-updater.argoproj.io/image-list: mcp-gateway=ghcr.io/stig-johnny/mcp-server-dotnet/mcp-gateway
    argocd-image-updater.argoproj.io/mcp-gateway.update-strategy: latest
    argocd-image-updater.argoproj.io/mcp-gateway.allow-tags: regexp:^(main|develop|v.*)$
    argocd-image-updater.argoproj.io/mcp-gateway.helm.image-name: gateway.image.repository
    argocd-image-updater.argoproj.io/mcp-gateway.helm.image-tag: gateway.image.tag
    argocd-image-updater.argoproj.io/mcp-gateway.pull-secret: pullsecret:argocd/ghcr-secret
    argocd-image-updater.argoproj.io/mcp-gateway.force-update: "true"
    argocd-image-updater.argoproj.io/write-back-method: git
    argocd-image-updater.argoproj.io/git-branch: main
    argocd-image-updater.argoproj.io/write-back-target: kustomization:helm/mcp-server-dotnet
  finalizers:
    - resources-finalizer.argocd.argoproj.io
spec:
  project: default
  source:
    repoURL: https://github.com/Stig-Johnny/mcp-server-dotnet.git
    targetRevision: main
    path: helm/mcp-server-dotnet
    helm:
      valueFiles:
        - values-gateway.yaml
      parameters:
        - name: global.tag
          value: main
        - name: gateway.enabled
          value: "true"
        - name: gateway.replicaCount
          value: "3"
  destination:
    server: https://kubernetes.default.svc
    namespace: mcp-gateway
  syncPolicy:
    automated:
      prune: true
      selfHeal: true
      allowEmpty: false
    syncOptions:
      - CreateNamespace=true
      - PrunePropagationPolicy=foreground
      - PruneLast=true
      - RespectIgnoreDifferences=true
    retry:
      limit: 5
      backoff:
        duration: 10s
        factor: 2
        maxDuration: 5m
  revisionHistoryLimit: 10
  ignoreDifferences:
    - group: apps
      kind: Deployment
      managedFieldsManagers:
        - kube-controller-manager
  info:
    - name: Purpose
      value: MCP Protocol Gateway for Model Context Protocol compliance
    - name: Environment
      value: Production
    - name: Documentation
      value: https://github.com/Stig-Johnny/mcp-server-dotnet/blob/main/docs/argocd-deployment.md
```

### 3. Full Application Manifest

Create the comprehensive application manifest for all services:

```yaml
# File: argocd/application.yaml
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: mcp-server-dotnet
  namespace: argocd
  labels:
    app.kubernetes.io/name: mcp-server-dotnet
  annotations:
    # ArgoCD Image Updater configuration for automatic Docker image updates
    argocd-image-updater.argoproj.io/image-list: |
      mcp-api=ghcr.io/stig-johnny/mcp-server-dotnet/mcp-server-api,
      mcp-bff=ghcr.io/stig-johnny/mcp-server-dotnet/mcp-server-bff,
      mcp-gateway=ghcr.io/stig-johnny/mcp-server-dotnet/mcp-gateway
    argocd-image-updater.argoproj.io/mcp-api.update-strategy: latest
    argocd-image-updater.argoproj.io/mcp-api.allow-tags: regexp:^(main|develop|v.*)$
    argocd-image-updater.argoproj.io/mcp-api.helm.image-name: api.image.repository
    argocd-image-updater.argoproj.io/mcp-api.helm.image-tag: api.image.tag
    argocd-image-updater.argoproj.io/mcp-api.pull-secret: pullsecret:argocd/ghcr-secret
    argocd-image-updater.argoproj.io/mcp-bff.update-strategy: latest
    argocd-image-updater.argoproj.io/mcp-bff.allow-tags: regexp:^(main|develop|v.*)$
    argocd-image-updater.argoproj.io/mcp-bff.helm.image-name: bff.image.repository
    argocd-image-updater.argoproj.io/mcp-bff.helm.image-tag: bff.image.tag
    argocd-image-updater.argoproj.io/mcp-bff.pull-secret: pullsecret:argocd/ghcr-secret
    argocd-image-updater.argoproj.io/mcp-gateway.update-strategy: latest
    argocd-image-updater.argoproj.io/mcp-gateway.allow-tags: regexp:^(main|develop|v.*)$
    argocd-image-updater.argoproj.io/mcp-gateway.helm.image-name: gateway.image.repository
    argocd-image-updater.argoproj.io/mcp-gateway.helm.image-tag: gateway.image.tag
    argocd-image-updater.argoproj.io/mcp-gateway.pull-secret: pullsecret:argocd/ghcr-secret
    argocd-image-updater.argoproj.io/write-back-method: git
    argocd-image-updater.argoproj.io/git-branch: main
    argocd-image-updater.argoproj.io/write-back-target: kustomization:helm/mcp-server-dotnet
  finalizers:
    - resources-finalizer.argocd.argoproj.io
spec:
  project: default
  source:
    repoURL: https://github.com/Stig-Johnny/mcp-server-dotnet.git
    targetRevision: main
    path: helm/mcp-server-dotnet
    helm:
      valueFiles:
        - values.yaml
      parameters:
        - name: global.tag
          value: main
  destination:
    server: https://kubernetes.default.svc
    namespace: mcp-server
  syncPolicy:
    automated:
      prune: true
      selfHeal: true
      allowEmpty: false
    syncOptions:
      - CreateNamespace=true
      - PrunePropagationPolicy=foreground
      - PruneLast=true
    retry:
      limit: 5
      backoff:
        duration: 5s
        factor: 2
        maxDuration: 3m
  revisionHistoryLimit: 10
```

## Sync Policies Configuration

### Understanding Sync Policies

ArgoCD sync policies control how and when applications are synchronized with their desired state. The MCP Gateway deployment uses sophisticated sync policies for reliable automation.

### Automated Sync Configuration

```yaml
syncPolicy:
  automated:
    prune: true          # Remove resources not defined in Git
    selfHeal: true       # Automatically fix drift
    allowEmpty: false    # Prevent empty applications
```

**Key Components:**

1. **Prune**: Automatically removes Kubernetes resources that are no longer defined in Git
2. **Self-Heal**: Automatically corrects any manual changes to resources
3. **Allow Empty**: Prevents deployment of empty applications

### Sync Options

```yaml
syncOptions:
  - CreateNamespace=true              # Auto-create target namespace
  - PrunePropagationPolicy=foreground # Wait for resource deletion
  - PruneLast=true                    # Delete resources in correct order
  - RespectIgnoreDifferences=true     # Honor ignoreDifferences settings
```

**Sync Options Explained:**

- **CreateNamespace**: Automatically creates the target namespace if it doesn't exist
- **PrunePropagationPolicy**: Controls how resource deletion is handled
- **PruneLast**: Ensures proper ordering during resource cleanup
- **RespectIgnoreDifferences**: Honors fields that should be ignored during sync

### Retry Logic

```yaml
retry:
  limit: 5           # Maximum retry attempts
  backoff:
    duration: 10s    # Initial delay between retries
    factor: 2        # Exponential backoff factor
    maxDuration: 5m  # Maximum delay between retries
```

**Retry Strategy:**
- **Progressive Backoff**: 10s → 20s → 40s → 80s → 160s
- **Maximum Duration**: Caps retry delays at 5 minutes
- **Failure Handling**: Stops retrying after 5 failed attempts

### Ignore Differences

```yaml
ignoreDifferences:
  - group: apps
    kind: Deployment
    managedFieldsManagers:
      - kube-controller-manager
```

**Purpose**: Prevents ArgoCD from detecting false drift due to Kubernetes controller updates.

## ArgoCD Image Updater Integration

### Overview

ArgoCD Image Updater automatically monitors container registries and updates application manifests when new images are available. This feature was added in PR #7 to enable zero-touch deployments.

### Image Updater Annotations

#### Basic Configuration

```yaml
# Monitor specific image
argocd-image-updater.argoproj.io/image-list: mcp-gateway=ghcr.io/stig-johnny/mcp-server-dotnet/mcp-gateway

# Update strategy
argocd-image-updater.argoproj.io/mcp-gateway.update-strategy: latest

# Allowed tags (regex pattern)
argocd-image-updater.argoproj.io/mcp-gateway.allow-tags: regexp:^(main|develop|v.*)$
```

#### Helm Integration

```yaml
# Helm value paths for image updates
argocd-image-updater.argoproj.io/mcp-gateway.helm.image-name: gateway.image.repository
argocd-image-updater.argoproj.io/mcp-gateway.helm.image-tag: gateway.image.tag

# Container registry authentication
argocd-image-updater.argoproj.io/mcp-gateway.pull-secret: pullsecret:argocd/ghcr-secret
```

#### Git Integration

```yaml
# Git write-back configuration
argocd-image-updater.argoproj.io/write-back-method: git
argocd-image-updater.argoproj.io/git-branch: main
argocd-image-updater.argoproj.io/write-back-target: kustomization:helm/mcp-server-dotnet
```

### Multi-Service Configuration

For the full application with multiple services:

```yaml
argocd-image-updater.argoproj.io/image-list: |
  mcp-api=ghcr.io/stig-johnny/mcp-server-dotnet/mcp-server-api,
  mcp-bff=ghcr.io/stig-johnny/mcp-server-dotnet/mcp-server-bff,
  mcp-gateway=ghcr.io/stig-johnny/mcp-server-dotnet/mcp-gateway
```

**Per-Service Configuration:**
```yaml
# API Service
argocd-image-updater.argoproj.io/mcp-api.update-strategy: latest
argocd-image-updater.argoproj.io/mcp-api.allow-tags: regexp:^(main|develop|v.*)$
argocd-image-updater.argoproj.io/mcp-api.helm.image-name: api.image.repository
argocd-image-updater.argoproj.io/mcp-api.helm.image-tag: api.image.tag

# BFF Service
argocd-image-updater.argoproj.io/mcp-bff.update-strategy: latest
argocd-image-updater.argoproj.io/mcp-bff.allow-tags: regexp:^(main|develop|v.*)$
argocd-image-updater.argoproj.io/mcp-bff.helm.image-name: bff.image.repository
argocd-image-updater.argoproj.io/mcp-bff.helm.image-tag: bff.image.tag

# Gateway Service
argocd-image-updater.argoproj.io/mcp-gateway.update-strategy: latest
argocd-image-updater.argoproj.io/mcp-gateway.allow-tags: regexp:^(main|develop|v.*)$
argocd-image-updater.argoproj.io/mcp-gateway.helm.image-name: gateway.image.repository
argocd-image-updater.argoproj.io/mcp-gateway.helm.image-tag: gateway.image.tag
```

### Update Strategies

| Strategy | Description | Use Case |
|----------|-------------|----------|
| `latest` | Always use the latest image | Development/Main branch |
| `semver` | Follow semantic versioning | Production releases |
| `digest` | Use image digests | Maximum security |

### Tag Filtering

```bash
# Allow main, develop, and version tags
regexp:^(main|develop|v.*)$

# Allow only version tags
regexp:^v\d+\.\d+\.\d+$

# Allow specific branches
regexp:^(main|staging)$
```

## Deployment Procedures

### 1. Initial Deployment

#### Step 1: Deploy AppProject (Optional)

```bash
# Apply the AppProject for enhanced security
kubectl apply -f argocd/appproject.yaml

# Verify AppProject creation
kubectl get appprojects -n argocd
```

#### Step 2: Deploy MCP Gateway

```bash
# Deploy the dedicated Gateway application
kubectl apply -f argocd/application-gateway.yaml

# Verify application creation
kubectl get applications -n argocd
```

#### Step 3: Deploy Full Application (Optional)

```bash
# Deploy the full application stack
kubectl apply -f argocd/application.yaml

# List all applications
kubectl get applications -n argocd
```

### 2. Validation Deployment

Before production deployment, validate your configuration:

```bash
# Run comprehensive deployment tests
./scripts/test-deployment.sh
```

**Test Coverage:**
- ✅ .NET application builds successfully
- ✅ MCP Protocol compliance tests pass
- ✅ Docker container builds successfully
- ✅ Helm charts are valid and render correctly
- ✅ ArgoCD configurations include image updater
- ✅ GitHub Actions workflow includes gateway build

### 3. Environment-Specific Deployments

#### Development Environment

```bash
# Deploy to development with dev tag
kubectl apply -f - <<EOF
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: mcp-gateway-dev
  namespace: argocd
spec:
  project: default
  source:
    repoURL: https://github.com/Stig-Johnny/mcp-server-dotnet.git
    targetRevision: develop
    path: helm/mcp-server-dotnet
    helm:
      valueFiles:
        - values-gateway.yaml
        - values-dev.yaml
      parameters:
        - name: global.tag
          value: develop
  destination:
    server: https://kubernetes.default.svc
    namespace: mcp-gateway-dev
  syncPolicy:
    automated:
      prune: true
      selfHeal: true
EOF
```

#### Production Environment

```bash
# Deploy to production with version tag
kubectl apply -f - <<EOF
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: mcp-gateway-prod
  namespace: argocd
spec:
  project: mcp-server
  source:
    repoURL: https://github.com/Stig-Johnny/mcp-server-dotnet.git
    targetRevision: main
    path: helm/mcp-server-dotnet
    helm:
      valueFiles:
        - values-gateway.yaml
        - values-prod.yaml
      parameters:
        - name: global.tag
          value: v1.0.0
  destination:
    server: https://kubernetes.default.svc
    namespace: mcp-gateway
  syncPolicy:
    automated:
      prune: true
      selfHeal: true
EOF
```

## Application Status Verification

### 1. ArgoCD Application Status

#### Check Application Health

```bash
# List all applications
kubectl get applications -n argocd

# Get detailed application status
kubectl describe application mcp-gateway -n argocd

# Check application sync status
kubectl get application mcp-gateway -n argocd -o jsonpath='{.status.sync.status}'
```

**Expected Output:**
```
NAME         SYNC STATUS   HEALTH STATUS   REPO                                              PATH                    TARGET
mcp-gateway  Synced        Healthy         https://github.com/Stig-Johnny/mcp-server-dotnet helm/mcp-server-dotnet main
```

#### Using ArgoCD CLI

```bash
# Install ArgoCD CLI
curl -sSL -o argocd-linux-amd64 https://github.com/argoproj/argo-cd/releases/latest/download/argocd-linux-amd64
sudo install -m 555 argocd-linux-amd64 /usr/local/bin/argocd

# Login to ArgoCD
argocd login <argocd-server>

# Get application status
argocd app get mcp-gateway

# List application resources
argocd app resources mcp-gateway

# View application logs
argocd app logs mcp-gateway
```

### 2. Kubernetes Resource Status

#### Check Deployment Status

```bash
# Check Gateway deployment
kubectl get deployments -n mcp-gateway

# Check pods status
kubectl get pods -n mcp-gateway

# Check service endpoints
kubectl get services -n mcp-gateway

# Check ingress (if configured)
kubectl get ingress -n mcp-gateway
```

#### Detailed Resource Inspection

```bash
# Describe Gateway deployment
kubectl describe deployment mcp-server-dotnet-gateway -n mcp-gateway

# Check pod logs
kubectl logs -l app.kubernetes.io/component=gateway -n mcp-gateway

# Check resource usage
kubectl top pods -n mcp-gateway
```

### 3. Health Check Verification

#### Gateway Health Endpoints

```bash
# Port forward to Gateway service
kubectl port-forward svc/mcp-server-dotnet-gateway 8080:80 -n mcp-gateway

# Check health endpoint
curl http://localhost:8080/health

# Check readiness endpoint
curl http://localhost:8080/health/ready

# Check MCP protocol endpoint
curl http://localhost:8080/api/mcp/tools
```

**Expected Health Response:**
```json
{
  "status": "Healthy",
  "results": {
    "mcp-protocol": {
      "status": "Healthy",
      "description": "MCP Protocol endpoints are responding"
    }
  }
}
```

### 4. ArgoCD Image Updater Status

#### Monitor Image Updates

```bash
# Check Image Updater pods
kubectl get pods -n argocd -l app.kubernetes.io/name=argocd-image-updater

# View Image Updater logs
kubectl logs -n argocd -l app.kubernetes.io/name=argocd-image-updater -f

# Check application annotations for update status
kubectl get application mcp-gateway -n argocd -o jsonpath='{.metadata.annotations}'
```

#### Image Update History

```bash
# Check application history
argocd app history mcp-gateway

# View specific revision
argocd app get mcp-gateway --revision <REVISION>

# Check Git commits for image updates
git log --oneline --grep="update.*image"
```

### 5. End-to-End Testing

#### Protocol Compliance Testing

```bash
# Test MCP protocol endpoints
curl -X GET http://localhost:8080/api/mcp/tools | jq '.'

# Test tool execution
curl -X POST http://localhost:8080/api/mcp/tools/echo/execute \
  -H "Content-Type: application/json" \
  -d '{"message": "Hello MCP Gateway"}'

# Test resource endpoints
curl -X GET http://localhost:8080/api/mcp/resources | jq '.'
```

#### Load Testing (Optional)

```bash
# Install hey for load testing
go install github.com/rakyll/hey@latest

# Perform load test
hey -n 1000 -c 10 http://localhost:8080/health

# Test with concurrent requests
hey -n 100 -c 5 -m POST -T application/json \
  -d '{"message":"test"}' \
  http://localhost:8080/api/mcp/tools/echo/execute
```

## Troubleshooting

### Common Issues and Solutions

#### 1. Application Stuck in Progressing State

**Symptoms:**
```bash
kubectl get application mcp-gateway -n argocd
# Shows: SYNC STATUS = OutOfSync, HEALTH STATUS = Progressing
```

**Solutions:**
```bash
# Check application events
kubectl describe application mcp-gateway -n argocd

# Manual sync
argocd app sync mcp-gateway

# Hard refresh
argocd app sync mcp-gateway --force

# Check pod status
kubectl get pods -n mcp-gateway
kubectl describe pods -l app.kubernetes.io/name=mcp-server-dotnet -n mcp-gateway
```

#### 2. Image Updater Not Working

**Symptoms:**
- No automatic image updates
- ArgoCD Image Updater logs show errors

**Diagnostics:**
```bash
# Check Image Updater logs
kubectl logs -n argocd -l app.kubernetes.io/name=argocd-image-updater --tail=100

# Verify pull secret
kubectl get secret ghcr-secret -n argocd -o jsonpath='{.data}'

# Check registry connectivity
kubectl run test-registry --rm -i --tty --image=curlimages/curl -- \
  curl -I https://ghcr.io/v2/
```

**Solutions:**
```bash
# Recreate pull secret
kubectl delete secret ghcr-secret -n argocd
kubectl create secret docker-registry ghcr-secret \
  --docker-server=ghcr.io \
  --docker-username=<username> \
  --docker-password=<token> \
  --namespace=argocd

# Restart Image Updater
kubectl rollout restart deployment argocd-image-updater -n argocd

# Force image update annotation
kubectl annotate application mcp-gateway -n argocd \
  argocd-image-updater.argoproj.io/mcp-gateway.force-update="$(date +%s)"
```

#### 3. Sync Policy Issues

**Symptoms:**
- Manual changes being reverted unexpectedly
- Resources not being pruned

**Solutions:**
```bash
# Temporarily disable auto-sync
kubectl patch application mcp-gateway -n argocd -p \
  '{"spec":{"syncPolicy":{"automated":null}}}' --type merge

# Check ignore differences
kubectl get application mcp-gateway -n argocd -o jsonpath='{.spec.ignoreDifferences}'

# Re-enable auto-sync
kubectl patch application mcp-gateway -n argocd -p \
  '{"spec":{"syncPolicy":{"automated":{"prune":true,"selfHeal":true}}}}' --type merge
```

#### 4. Namespace Creation Issues

**Symptoms:**
- Application deployment fails due to missing namespace

**Solutions:**
```bash
# Manually create namespace
kubectl create namespace mcp-gateway

# Verify syncOptions includes CreateNamespace
kubectl get application mcp-gateway -n argocd -o jsonpath='{.spec.syncPolicy.syncOptions}'

# Add CreateNamespace sync option if missing
kubectl patch application mcp-gateway -n argocd -p \
  '{"spec":{"syncPolicy":{"syncOptions":["CreateNamespace=true"]}}}' --type merge
```

### Debug Commands Reference

```bash
# Application debugging
argocd app get mcp-gateway --show-managed-fields
argocd app diff mcp-gateway
argocd app sync mcp-gateway --dry-run

# Kubernetes debugging
kubectl get events -n mcp-gateway --sort-by='.lastTimestamp'
kubectl logs -l app.kubernetes.io/name=mcp-server-dotnet -n mcp-gateway --previous
kubectl describe pods -l app.kubernetes.io/component=gateway -n mcp-gateway

# ArgoCD debugging
kubectl logs -n argocd -l app.kubernetes.io/name=argocd-application-controller
kubectl logs -n argocd -l app.kubernetes.io/name=argocd-server
kubectl logs -n argocd -l app.kubernetes.io/name=argocd-repo-server
```

## Best Practices

### 1. Application Organization

- **Use separate applications** for different environments (dev, staging, prod)
- **Implement AppProjects** for multi-tenant scenarios
- **Use meaningful labels** for application categorization
- **Document application purpose** in annotations

### 2. Sync Policy Configuration

- **Enable pruning** for clean deployments
- **Use self-heal cautiously** in production
- **Configure appropriate retry limits** to avoid excessive retries
- **Test sync policies** in development environments first

### 3. Image Update Management

- **Use specific tag patterns** to control which images get updated
- **Test image updates** in development before production
- **Monitor update logs** regularly
- **Have rollback procedures** ready

### 4. Security Considerations

- **Use AppProjects** to limit resource access
- **Secure container registry access** with proper authentication
- **Regular secret rotation** for registry credentials
- **Enable RBAC** for ArgoCD access control

### 5. Monitoring and Alerting

- **Set up alerts** for application sync failures
- **Monitor Image Updater** for update failures
- **Track deployment metrics** for performance insights
- **Regular health checks** for all components

### 6. GitOps Workflow

- **All changes through Git** - no manual kubectl modifications
- **Use proper Git branching** strategy
- **Review image update commits** before merging
- **Tag releases** for stable deployments

### 7. Disaster Recovery

- **Backup ArgoCD configuration** regularly
- **Document recovery procedures** for all scenarios
- **Test disaster recovery** procedures periodically
- **Have rollback plans** for failed deployments

---

This documentation provides comprehensive guidance for automating MCP Gateway deployments with ArgoCD, incorporating all the enhancements from pull request #7, including ArgoCD Image Updater integration for zero-touch container image updates.