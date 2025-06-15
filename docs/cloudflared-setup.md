# Cloudflare Tunnel Setup for stigjohnny.no

This document provides step-by-step instructions for setting up Cloudflare Tunnels with the MCP Server .NET deployment for the stigjohnny.no domain.

## Prerequisites

- Cloudflare account with stigjohnny.no domain configured
- `cloudflared` CLI tool installed locally
- kubectl access to the target Kubernetes cluster
- ArgoCD deployed and configured

## Setup Process

### 1. Create Cloudflare Tunnels

Create separate tunnels for each environment:

```bash
# Production tunnel
cloudflared tunnel create mcp-server-prod

# Staging tunnel  
cloudflared tunnel create mcp-server-staging

# Development tunnel
cloudflared tunnel create mcp-server-dev
```

Note the tunnel IDs from the output - you'll need these for the next steps.

### 2. Configure DNS Records

Set up DNS records for each environment:

```bash
# Production (replace TUNNEL_ID_PROD with actual tunnel ID)
cloudflared tunnel route dns TUNNEL_ID_PROD mcp-server.stigjohnny.no
cloudflared tunnel route dns TUNNEL_ID_PROD mcp-gateway.stigjohnny.no
cloudflared tunnel route dns TUNNEL_ID_PROD mcp-api.stigjohnny.no

# Staging (replace TUNNEL_ID_STAGING with actual tunnel ID)
cloudflared tunnel route dns TUNNEL_ID_STAGING mcp-staging.stigjohnny.no
cloudflared tunnel route dns TUNNEL_ID_STAGING mcp-gateway-staging.stigjohnny.no
cloudflared tunnel route dns TUNNEL_ID_STAGING mcp-api-staging.stigjohnny.no

# Development (replace TUNNEL_ID_DEV with actual tunnel ID)
cloudflared tunnel route dns TUNNEL_ID_DEV mcp-dev.stigjohnny.no
cloudflared tunnel route dns TUNNEL_ID_DEV mcp-gateway-dev.stigjohnny.no
cloudflared tunnel route dns TUNNEL_ID_DEV mcp-api-dev.stigjohnny.no
```

### 3. Create Kubernetes Secrets

Create the tunnel credential secrets in each namespace:

```bash
# Production environment
kubectl create namespace mcp-server --dry-run=client -o yaml | kubectl apply -f -
kubectl create secret generic mcp-server-dotnet-cloudflared-creds \
  --from-file=credentials.json=~/.cloudflared/TUNNEL_ID_PROD.json \
  --namespace=mcp-server

# If you have staging/dev environments in separate namespaces, repeat as needed
# Example for staging namespace:
# kubectl create namespace mcp-server-staging --dry-run=client -o yaml | kubectl apply -f -
# kubectl create secret generic mcp-server-dotnet-cloudflared-creds \
#   --from-file=credentials.json=~/.cloudflared/TUNNEL_ID_STAGING.json \
#   --namespace=mcp-server-staging
```

### 4. Update Helm Values

Update the Helm values files with actual tunnel IDs:

```bash
# Edit production values
sed -i 's/REPLACE_WITH_ACTUAL_TUNNEL_ID/TUNNEL_ID_PROD/g' helm/mcp-server-dotnet/values-prod.yaml

# Edit staging values  
sed -i 's/REPLACE_WITH_ACTUAL_TUNNEL_ID/TUNNEL_ID_STAGING/g' helm/mcp-server-dotnet/values-staging.yaml

# Edit development values
sed -i 's/REPLACE_WITH_ACTUAL_TUNNEL_ID/TUNNEL_ID_DEV/g' helm/mcp-server-dotnet/values-dev.yaml
```

### 5. Deploy via GitOps

Commit the changes and let ArgoCD handle the deployment:

```bash
# Commit the updated values
git add helm/mcp-server-dotnet/values-*.yaml
git commit -m "Configure cloudflared tunnel IDs for stigjohnny.no"
git push origin main

# Deploy the app-of-apps pattern
kubectl apply -f argocd/app-of-apps.yaml

# Monitor the deployment
kubectl get applications -n argocd
kubectl get pods -n mcp-server
```

### 6. Verify the Setup

After deployment, verify that the tunnels are working:

```bash
# Check cloudflared pods
kubectl get pods -n mcp-server -l app.kubernetes.io/component=cloudflared

# Check tunnel status
kubectl logs -n mcp-server -l app.kubernetes.io/component=cloudflared

# Test external access
curl -k https://mcp-server.stigjohnny.no/health
curl -k https://mcp-gateway.stigjohnny.no/api/mcp/tools
```

## Troubleshooting

### Common Issues

1. **Tunnel credentials not found**:
   - Verify the secret exists: `kubectl get secret mcp-server-dotnet-cloudflared-creds -n mcp-server`
   - Check the credentials file path: `ls ~/.cloudflared/`

2. **DNS not resolving**:
   - Verify DNS records: `nslookup mcp-server.stigjohnny.no`
   - Check Cloudflare DNS settings in the dashboard

3. **Service not accessible**:
   - Check pod logs: `kubectl logs -n mcp-server -l app.kubernetes.io/component=cloudflared`
   - Verify internal services: `kubectl get svc -n mcp-server`

### Useful Commands

```bash
# View tunnel configuration
kubectl get configmap mcp-server-dotnet-cloudflared-config -n mcp-server -o yaml

# Check tunnel metrics
kubectl port-forward -n mcp-server svc/cloudflared-metrics 2000:2000
curl http://localhost:2000/metrics

# Restart cloudflared deployment
kubectl rollout restart deployment/mcp-server-dotnet-cloudflared -n mcp-server
```

## Security Considerations

- Tunnel credentials contain sensitive information - store them securely
- Use separate tunnels for each environment
- Regularly rotate tunnel credentials if compromised
- Monitor tunnel logs for unusual activity
- Consider using external secret management for production

## Domain Configuration Summary

| Environment | BFF Service | Gateway Service | API Service |
|-------------|-------------|----------------|-------------|
| Production | mcp-server.stigjohnny.no | mcp-gateway.stigjohnny.no | mcp-api.stigjohnny.no |
| Staging | mcp-staging.stigjohnny.no | mcp-gateway-staging.stigjohnny.no | mcp-api-staging.stigjohnny.no |
| Development | mcp-dev.stigjohnny.no | mcp-gateway-dev.stigjohnny.no | mcp-api-dev.stigjohnny.no |