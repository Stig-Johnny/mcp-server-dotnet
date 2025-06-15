#!/bin/bash
# Interactive ArgoCD Deployment Script for MCP Server .NET
# This script provides an interactive deployment experience that prompts for secrets
# and handles the complete deployment workflow for public and private repositories.

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
PURPLE='\033[0;35m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
ARGOCD_NAMESPACE="argocd"

# Functions
print_header() {
    echo -e "${PURPLE}╔══════════════════════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${PURPLE}║                     MCP Server .NET ArgoCD Deployment                       ║${NC}"
    echo -e "${PURPLE}║                        Interactive Setup Script                             ║${NC}"
    echo -e "${PURPLE}╚══════════════════════════════════════════════════════════════════════════════╝${NC}"
    echo ""
}

print_step() {
    echo -e "${CYAN}📋 $1${NC}"
    echo ""
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

print_info() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

# Check if command exists
check_command() {
    if ! command -v "$1" &> /dev/null; then
        print_error "$1 is not installed or not in PATH"
        exit 1
    fi
}

# Check if command exists (optional)
check_optional_command() {
    if ! command -v "$1" &> /dev/null; then
        return 1
    fi
    return 0
}

# Check prerequisites
check_prerequisites() {
    print_step "Checking prerequisites..."
    
    check_command "kubectl"
    check_command "git"
    
    # Check if kubectl can connect to cluster
    if ! kubectl cluster-info > /dev/null 2>&1; then
        print_error "Cannot connect to Kubernetes cluster. Please check your kubectl configuration."
        exit 1
    fi
    
    print_success "All prerequisites satisfied"
    echo ""
}

# Check if ArgoCD is installed
check_argocd() {
    print_step "Checking ArgoCD installation..."
    
    if ! kubectl get namespace "$ARGOCD_NAMESPACE" > /dev/null 2>&1; then
        print_error "ArgoCD namespace '$ARGOCD_NAMESPACE' not found."
        echo ""
        echo "Please install ArgoCD first:"
        echo "kubectl create namespace argocd"
        echo "kubectl apply -n argocd -f https://raw.githubusercontent.com/argoproj/argo-cd/stable/manifests/install.yaml"
        exit 1
    fi
    
    if ! kubectl get deployment argocd-server -n "$ARGOCD_NAMESPACE" > /dev/null 2>&1; then
        print_error "ArgoCD server deployment not found."
        echo "Please install ArgoCD first."
        exit 1
    fi
    
    print_success "ArgoCD is installed"
    echo ""
}

# Prompt for yes/no input
prompt_yes_no() {
    local prompt="$1"
    local default="$2"
    local response
    
    while true; do
        if [ "$default" = "y" ]; then
            read -p "$prompt [Y/n]: " response
            response=${response:-y}
        else
            read -p "$prompt [y/N]: " response
            response=${response:-n}
        fi
        
        case "$response" in
            [Yy]|[Yy][Ee][Ss]) return 0 ;;
            [Nn]|[Nn][Oo]) return 1 ;;
            *) echo "Please answer yes or no." ;;
        esac
    done
}

# Prompt for input with validation
prompt_input() {
    local prompt="$1"
    local validate_func="$2"
    local allow_empty="$3"
    local response
    
    while true; do
        read -p "$prompt: " response
        
        if [ -z "$response" ] && [ "$allow_empty" = "true" ]; then
            echo ""
            return 0
        elif [ -z "$response" ]; then
            echo "This field cannot be empty. Please try again."
            continue
        fi
        
        if [ -n "$validate_func" ] && ! "$validate_func" "$response"; then
            continue
        fi
        
        echo "$response"
        return 0
    done
}

# Prompt for password/token input (hidden)
prompt_password() {
    local prompt="$1"
    local response
    
    while true; do
        read -s -p "$prompt: " response
        echo ""
        
        if [ -z "$response" ]; then
            echo "This field cannot be empty. Please try again."
            continue
        fi
        
        echo "$response"
        return 0
    done
}

# Validate GitHub username
validate_github_username() {
    local username="$1"
    if [[ ! "$username" =~ ^[a-zA-Z0-9]([a-zA-Z0-9-]*[a-zA-Z0-9])?$ ]]; then
        echo "Invalid GitHub username format. Please try again."
        return 1
    fi
    return 0
}

# Validate tunnel ID format
validate_tunnel_id() {
    local tunnel_id="$1"
    if [[ ! "$tunnel_id" =~ ^[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}$ ]]; then
        echo "Invalid tunnel ID format. Expected format: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
        return 1
    fi
    return 0
}

# Create namespace if it doesn't exist
create_namespace() {
    local namespace="$1"
    
    if ! kubectl get namespace "$namespace" > /dev/null 2>&1; then
        print_info "Creating namespace: $namespace"
        kubectl create namespace "$namespace"
        print_success "Created namespace: $namespace"
    else
        print_info "Namespace '$namespace' already exists"
    fi
}

# Create GitHub Container Registry pull secret
create_ghcr_secret() {
    local username="$1"
    local token="$2"
    local namespace="$3"
    local secret_name="ghcr-secret"
    
    # Check if secret already exists
    if kubectl get secret "$secret_name" -n "$namespace" > /dev/null 2>&1; then
        if prompt_yes_no "Secret '$secret_name' already exists in namespace '$namespace'. Replace it?" "n"; then
            kubectl delete secret "$secret_name" -n "$namespace"
        else
            return 0
        fi
    fi
    
    print_info "Creating GitHub Container Registry pull secret in namespace: $namespace"
    kubectl create secret docker-registry "$secret_name" \
        --docker-server=ghcr.io \
        --docker-username="$username" \
        --docker-password="$token" \
        --namespace="$namespace"
    
    print_success "Created pull secret: $secret_name"
}

# Setup container registry access
setup_registry_access() {
    print_step "Container Registry Setup"
    
    echo "This deployment can work with both public and private container images."
    echo "If you're using public images, you can skip this section."
    echo ""
    
    if prompt_yes_no "Do you need to set up access to private container registries?" "n"; then
        echo ""
        echo "Setting up GitHub Container Registry (ghcr.io) access..."
        echo ""
        
        local username
        local token
        
        username=$(prompt_input "GitHub username" "validate_github_username")
        echo ""
        echo "You need a GitHub Personal Access Token with 'read:packages' scope."
        echo "Generate one at: https://github.com/settings/tokens"
        echo ""
        token=$(prompt_password "GitHub Personal Access Token")
        echo ""
        
        # Create secrets in relevant namespaces
        local namespaces=("$ARGOCD_NAMESPACE" "mcp-server" "mcp-server-staging" "mcp-server-dev" "mcp-gateway" "mcp-gateway-staging" "mcp-gateway-dev")
        
        for namespace in "${namespaces[@]}"; do
            create_namespace "$namespace"
            create_ghcr_secret "$username" "$token" "$namespace"
        done
        
        print_success "Registry access configured"
    else
        print_info "Skipping registry setup - assuming public images"
    fi
    
    echo ""
}

# Setup ArgoCD Image Updater Git write-back
setup_git_writeback() {
    print_step "ArgoCD Image Updater Git Write-back Setup"
    
    echo "ArgoCD Image Updater can automatically update image tags in your Git repository."
    echo "This requires write access to your repository."
    echo ""
    
    if prompt_yes_no "Do you want to enable automatic image updates with Git write-back?" "n"; then
        echo ""
        echo "Setting up Git write-back credentials..."
        echo ""
        
        local username
        local token
        local secret_name="argocd-image-updater-git-creds"
        
        username=$(prompt_input "GitHub username for write-back" "validate_github_username")
        echo ""
        echo "You need a GitHub Personal Access Token with 'repo' scope for write-back."
        echo "Generate one at: https://github.com/settings/tokens"
        echo ""
        token=$(prompt_password "GitHub Personal Access Token (with repo scope)")
        echo ""
        
        # Create Git credentials secret
        if kubectl get secret "$secret_name" -n "$ARGOCD_NAMESPACE" > /dev/null 2>&1; then
            if prompt_yes_no "Git write-back secret already exists. Replace it?" "n"; then
                kubectl delete secret "$secret_name" -n "$ARGOCD_NAMESPACE"
            else
                print_info "Using existing Git write-back secret"
                echo ""
                return
            fi
        fi
        
        print_info "Creating Git write-back secret..."
        kubectl create secret generic "$secret_name" \
            --from-literal=username="$username" \
            --from-literal=password="$token" \
            -n "$ARGOCD_NAMESPACE"
        
        # Label the secret so ArgoCD Image Updater can find it
        kubectl label secret "$secret_name" \
            app.kubernetes.io/part-of=argocd \
            -n "$ARGOCD_NAMESPACE"
        
        print_success "Git write-back credentials configured"
    else
        print_info "Skipping Git write-back setup"
    fi
    
    echo ""
}

# Setup Cloudflare Tunnels
setup_cloudflare_tunnels() {
    print_step "Cloudflare Tunnel Setup"
    
    echo "This setup can configure Cloudflare Tunnels for secure access to your MCP services"
    echo "via the stigjohnny.no domain without exposing your Kubernetes cluster directly."
    echo ""
    echo "Prerequisites:"
    echo "- Cloudflare account with stigjohnny.no domain configured"
    echo "- cloudflared CLI tool installed"
    echo ""
    
    if prompt_yes_no "Do you want to set up Cloudflare Tunnels?" "n"; then
        echo ""
        
        # Check if cloudflared is available
        if ! check_optional_command "cloudflared"; then
            print_error "cloudflared CLI tool is not installed or not in PATH"
            echo ""
            echo "Please install cloudflared first:"
            echo "Visit: https://developers.cloudflare.com/cloudflare-one/connections/connect-apps/install-and-setup/installation/"
            echo ""
            if prompt_yes_no "Continue without Cloudflare tunnel setup?" "y"; then
                return 0
            else
                exit 1
            fi
        fi
        
        # Check if user is logged in to Cloudflare
        if ! cloudflared tunnel list > /dev/null 2>&1; then
            print_error "Not authenticated with Cloudflare"
            echo ""
            echo "Please log in to Cloudflare first:"
            echo "cloudflared tunnel login"
            echo ""
            if prompt_yes_no "Continue without Cloudflare tunnel setup?" "y"; then
                return 0
            else
                exit 1
            fi
        fi
        
        echo "Setting up Cloudflare tunnels for stigjohnny.no domain..."
        echo ""
        
        # Create tunnels for each environment
        local environments=("prod" "staging" "dev")
        local tunnel_names=("mcp-server-prod" "mcp-server-staging" "mcp-server-dev")
        local tunnel_ids=()
        
        for i in "${!environments[@]}"; do
            local env="${environments[$i]}"
            local tunnel_name="${tunnel_names[$i]}"
            
            print_info "Creating tunnel for $env environment: $tunnel_name"
            
            # Check if tunnel already exists
            local existing_tunnel_id
            if command -v jq > /dev/null 2>&1; then
                existing_tunnel_id=$(cloudflared tunnel list --output json 2>/dev/null | jq -r ".[] | select(.name == \"$tunnel_name\") | .id" 2>/dev/null || echo "")
            else
                # Fallback if jq is not available - list tunnels and grep for name
                existing_tunnel_id=$(cloudflared tunnel list 2>/dev/null | grep "$tunnel_name" | awk '{print $1}' || echo "")
            fi
            
            if [ -n "$existing_tunnel_id" ] && [ "$existing_tunnel_id" != "null" ]; then
                print_warning "Tunnel '$tunnel_name' already exists with ID: $existing_tunnel_id"
                if prompt_yes_no "Use existing tunnel?" "y"; then
                    tunnel_ids+=("$existing_tunnel_id")
                    continue
                else
                    print_info "Creating new tunnel with different name..."
                    tunnel_name="${tunnel_name}-$(date +%s)"
                fi
            fi
            
            # Create the tunnel
            local tunnel_output
            tunnel_output=$(cloudflared tunnel create "$tunnel_name" 2>&1)
            
            if [ $? -eq 0 ]; then
                local tunnel_id
                tunnel_id=$(echo "$tunnel_output" | grep -o '[a-f0-9]\{8\}-[a-f0-9]\{4\}-[a-f0-9]\{4\}-[a-f0-9]\{4\}-[a-f0-9]\{12\}' | head -1)
                tunnel_ids+=("$tunnel_id")
                print_success "Created tunnel: $tunnel_name (ID: $tunnel_id)"
            else
                print_error "Failed to create tunnel: $tunnel_name"
                echo "$tunnel_output"
                if prompt_yes_no "Continue without this tunnel?" "n"; then
                    tunnel_ids+=("")
                    continue
                else
                    return 1
                fi
            fi
        done
        
        echo ""
        
        # Configure DNS records
        print_info "Configuring DNS records..."
        
        local domains_prod=("mcp-server.stigjohnny.no" "mcp-gateway.stigjohnny.no" "mcp-api.stigjohnny.no")
        local domains_staging=("mcp-staging.stigjohnny.no" "mcp-gateway-staging.stigjohnny.no" "mcp-api-staging.stigjohnny.no")
        local domains_dev=("mcp-dev.stigjohnny.no" "mcp-gateway-dev.stigjohnny.no" "mcp-api-dev.stigjohnny.no")
        
        local all_domains=("${domains_prod[@]}" "${domains_staging[@]}" "${domains_dev[@]}")
        local all_tunnel_ids=("${tunnel_ids[0]}" "${tunnel_ids[0]}" "${tunnel_ids[0]}" "${tunnel_ids[1]}" "${tunnel_ids[1]}" "${tunnel_ids[1]}" "${tunnel_ids[2]}" "${tunnel_ids[2]}" "${tunnel_ids[2]}")
        
        for i in "${!all_domains[@]}"; do
            local domain="${all_domains[$i]}"
            local tunnel_id="${all_tunnel_ids[$i]}"
            
            if [ -n "$tunnel_id" ]; then
                print_info "Setting up DNS for: $domain"
                if cloudflared tunnel route dns "$tunnel_id" "$domain" > /dev/null 2>&1; then
                    print_success "DNS configured for: $domain"
                else
                    print_warning "Failed to configure DNS for: $domain (may already exist)"
                fi
            fi
        done
        
        echo ""
        
        # Create Kubernetes secrets
        print_info "Creating Kubernetes secrets for tunnel credentials..."
        
        for i in "${!environments[@]}"; do
            local env="${environments[$i]}"
            local tunnel_id="${tunnel_ids[$i]}"
            
            if [ -n "$tunnel_id" ]; then
                local namespace
                case "$env" in
                    "prod") namespace="mcp-server" ;;
                    "staging") namespace="mcp-server-staging" ;;
                    "dev") namespace="mcp-server-dev" ;;
                esac
                
                local credentials_file="$HOME/.cloudflared/$tunnel_id.json"
                
                if [ -f "$credentials_file" ]; then
                    create_namespace "$namespace"
                    
                    local secret_name="mcp-server-dotnet-cloudflared-creds"
                    
                    # Delete existing secret if it exists
                    if kubectl get secret "$secret_name" -n "$namespace" > /dev/null 2>&1; then
                        kubectl delete secret "$secret_name" -n "$namespace"
                    fi
                    
                    # Create new secret
                    if kubectl create secret generic "$secret_name" \
                        --from-file=credentials.json="$credentials_file" \
                        --namespace="$namespace" > /dev/null 2>&1; then
                        print_success "Created tunnel credentials secret for $env environment"
                    else
                        print_error "Failed to create tunnel credentials secret for $env environment"
                    fi
                else
                    print_warning "Credentials file not found for $env tunnel: $credentials_file"
                fi
            fi
        done
        
        echo ""
        
        # Update Helm values files
        print_info "Updating Helm values files with tunnel IDs..."
        
        local values_files=("values-prod.yaml" "values-staging.yaml" "values-dev.yaml")
        
        for i in "${!values_files[@]}"; do
            local values_file="$REPO_ROOT/helm/mcp-server-dotnet/${values_files[$i]}"
            local tunnel_id="${tunnel_ids[$i]}"
            
            if [ -n "$tunnel_id" ] && [ -f "$values_file" ]; then
                # Replace the placeholder tunnel ID
                if sed -i.bak "s/REPLACE_WITH_ACTUAL_TUNNEL_ID/$tunnel_id/g" "$values_file"; then
                    print_success "Updated tunnel ID in ${values_files[$i]}"
                    
                    # Also enable cloudflared in the values file if it's not already enabled
                    if grep -q "enabled: false" "$values_file" | head -1; then
                        sed -i.bak "s/enabled: false/enabled: true/" "$values_file"
                    fi
                else
                    print_warning "Failed to update tunnel ID in ${values_files[$i]}"
                fi
            fi
        done
        
        echo ""
        print_success "Cloudflare tunnel setup completed!"
        
        echo ""
        echo "Summary of created tunnels:"
        for i in "${!environments[@]}"; do
            local env="${environments[$i]}"
            local tunnel_name="${tunnel_names[$i]}"
            local tunnel_id="${tunnel_ids[$i]}"
            
            if [ -n "$tunnel_id" ]; then
                echo "  $env: $tunnel_name (ID: $tunnel_id)"
            fi
        done
        
        echo ""
        echo "Next steps:"
        echo "1. Verify DNS propagation: nslookup mcp-server.stigjohnny.no"
        echo "2. Deploy applications to test tunnel connectivity"
        echo "3. Check tunnel status after deployment:"
        echo "   kubectl logs -n mcp-server -l app.kubernetes.io/component=cloudflared"
        
    else
        print_info "Skipping Cloudflare tunnel setup"
    fi
    
    echo ""
}

# Deploy ArgoCD applications
deploy_applications() {
    print_step "Deploying ArgoCD Applications"
    
    cd "$REPO_ROOT"
    
    # Check if app-of-apps exists
    if [ ! -f "argocd/app-of-apps.yaml" ]; then
        print_error "App-of-apps manifest not found: argocd/app-of-apps.yaml"
        exit 1
    fi
    
    echo "Deployment options:"
    echo "1. Deploy all applications (production, staging, and development)"
    echo "2. Deploy only production applications"
    echo "3. Deploy AppProject only (for security configuration)"
    echo ""
    
    local choice
    while true; do
        read -p "Select deployment option [1-3]: " choice
        case "$choice" in
            1)
                print_info "Deploying all applications using app-of-apps pattern..."
                kubectl apply -f argocd/app-of-apps.yaml
                break
                ;;
            2)
                print_info "Deploying production applications only..."
                kubectl apply -f argocd/applications/application.yaml
                kubectl apply -f argocd/applications/application-gateway.yaml
                break
                ;;
            3)
                if [ -f "argocd/appproject.yaml" ]; then
                    print_info "Deploying AppProject for security configuration..."
                    kubectl apply -f argocd/appproject.yaml
                else
                    print_warning "AppProject manifest not found"
                fi
                return
                ;;
            *)
                echo "Invalid choice. Please select 1, 2, or 3."
                continue
                ;;
        esac
    done
    
    print_success "Applications deployed successfully"
    echo ""
}

# Wait for applications to be ready
wait_for_applications() {
    print_step "Waiting for applications to be ready..."
    
    local max_wait=300  # 5 minutes
    local wait_time=0
    local check_interval=10
    
    while [ $wait_time -lt $max_wait ]; do
        local apps_ready=true
        
        # Get all applications in argocd namespace
        local apps
        apps=$(kubectl get applications -n "$ARGOCD_NAMESPACE" -o jsonpath='{.items[*].metadata.name}' 2>/dev/null || echo "")
        
        if [ -z "$apps" ]; then
            print_info "No applications found yet, waiting..."
            sleep $check_interval
            wait_time=$((wait_time + check_interval))
            continue
        fi
        
        for app in $apps; do
            local sync_status
            local health_status
            
            sync_status=$(kubectl get application "$app" -n "$ARGOCD_NAMESPACE" -o jsonpath='{.status.sync.status}' 2>/dev/null || echo "Unknown")
            health_status=$(kubectl get application "$app" -n "$ARGOCD_NAMESPACE" -o jsonpath='{.status.health.status}' 2>/dev/null || echo "Unknown")
            
            if [ "$sync_status" != "Synced" ] || [ "$health_status" != "Healthy" ]; then
                apps_ready=false
                print_info "Application '$app' - Sync: $sync_status, Health: $health_status"
            fi
        done
        
        if $apps_ready; then
            print_success "All applications are synced and healthy!"
            return 0
        fi
        
        sleep $check_interval
        wait_time=$((wait_time + check_interval))
        echo ""
    done
    
    print_warning "Timeout waiting for applications to be ready. Check status manually."
    return 1
}

# Display deployment status
show_deployment_status() {
    print_step "Deployment Status"
    
    echo "ArgoCD Applications:"
    kubectl get applications -n "$ARGOCD_NAMESPACE" -o wide 2>/dev/null || echo "No applications found"
    echo ""
    
    echo "Deployed Pods:"
    local namespaces=("mcp-server" "mcp-server-staging" "mcp-server-dev" "mcp-gateway" "mcp-gateway-staging" "mcp-gateway-dev")
    
    for namespace in "${namespaces[@]}"; do
        if kubectl get namespace "$namespace" > /dev/null 2>&1; then
            local pod_count
            pod_count=$(kubectl get pods -n "$namespace" --no-headers 2>/dev/null | wc -l)
            if [ "$pod_count" -gt 0 ]; then
                echo "Namespace: $namespace"
                kubectl get pods -n "$namespace" -o wide 2>/dev/null || echo "No pods found"
                echo ""
            fi
        fi
    done
}

# Show next steps
show_next_steps() {
    print_step "Next Steps"
    
    echo "🎉 Deployment completed! Here's what you can do next:"
    echo ""
    echo "1. Monitor applications:"
    echo "   kubectl get applications -n argocd"
    echo ""
    echo "2. Check application logs:"
    echo "   kubectl logs -l app.kubernetes.io/name=mcp-server-dotnet -n mcp-server"
    echo ""
    echo "3. Access ArgoCD UI (if available):"
    echo "   kubectl port-forward svc/argocd-server -n argocd 8080:443"
    echo "   # Then visit: https://localhost:8080"
    echo ""
    echo "4. Test the MCP Gateway:"
    echo "   kubectl port-forward svc/mcp-server-dotnet-gateway -n mcp-gateway 8081:80"
    echo "   curl http://localhost:8081/health"
    echo ""
    echo "5. View this deployment script help:"
    echo "   $0 --help"
    echo ""
    
    print_info "For troubleshooting, check the documentation at:"
    print_info "docs/argocd-deployment.md"
}

# Show help
show_help() {
    echo "Interactive ArgoCD Deployment Script for MCP Server .NET"
    echo ""
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  --help, -h          Show this help message"
    echo "  --skip-registry     Skip container registry setup"
    echo "  --skip-git          Skip Git write-back setup"
    echo "  --skip-cloudflare   Skip Cloudflare tunnel setup"
    echo "  --apps-only         Deploy applications only (skip secrets setup)"
    echo "  --status            Show deployment status only"
    echo ""
    echo "This script provides an interactive setup for deploying MCP Server .NET"
    echo "applications using ArgoCD. It will:"
    echo ""
    echo "1. Check prerequisites (kubectl, ArgoCD)"
    echo "2. Optionally set up container registry access"
    echo "3. Optionally set up Git write-back for image updates"
    echo "4. Optionally set up Cloudflare tunnels for stigjohnny.no"
    echo "5. Deploy ArgoCD applications"
    echo "6. Monitor deployment status"
    echo ""
    echo "For more information, see: docs/argocd-deployment.md"
}

# Main function
main() {
    # Parse command line arguments
    local skip_registry=false
    local skip_git=false
    local skip_cloudflare=false
    local apps_only=false
    local status_only=false
    
    while [[ $# -gt 0 ]]; do
        case $1 in
            --help|-h)
                show_help
                exit 0
                ;;
            --skip-registry)
                skip_registry=true
                shift
                ;;
            --skip-git)
                skip_git=true
                shift
                ;;
            --skip-cloudflare)
                skip_cloudflare=true
                shift
                ;;
            --apps-only)
                apps_only=true
                shift
                ;;
            --status)
                status_only=true
                shift
                ;;
            *)
                echo "Unknown option: $1"
                echo "Use --help for usage information"
                exit 1
                ;;
        esac
    done
    
    print_header
    
    if [ "$status_only" = true ]; then
        show_deployment_status
        exit 0
    fi
    
    check_prerequisites
    check_argocd
    
    if [ "$apps_only" = false ]; then
        if [ "$skip_registry" = false ]; then
            setup_registry_access
        fi
        
        if [ "$skip_git" = false ]; then
            setup_git_writeback
        fi
        
        # Add Cloudflare tunnel setup
        if [ "$skip_cloudflare" = false ]; then
            setup_cloudflare_tunnels
        fi
    fi
    
    deploy_applications
    
    if prompt_yes_no "Wait for applications to be ready?" "y"; then
        wait_for_applications
    fi
    
    show_deployment_status
    show_next_steps
    
    print_success "Interactive deployment completed!"
}

# Run main function with all arguments
main "$@"