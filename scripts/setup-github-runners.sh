#!/bin/bash

# GitHub Actions Self-Hosted Runners Setup Script
# This script helps set up the GitHub Actions self-hosted runners in Kubernetes

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
NAMESPACE="github-runners"
SECRET_NAME="github-actions-secret"
RUNNER_CONTROLLER_VERSION="latest"

# Functions
log_info() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

log_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

log_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

log_error() {
    echo -e "${RED}❌ $1${NC}"
}

check_prerequisites() {
    log_info "Checking prerequisites..."
    
    # Check kubectl
    if ! command -v kubectl &> /dev/null; then
        log_error "kubectl is not installed or not in PATH"
        exit 1
    fi
    log_success "kubectl is available"
    
    # Check cluster connectivity
    if ! kubectl cluster-info &> /dev/null; then
        log_error "Cannot connect to Kubernetes cluster"
        exit 1
    fi
    log_success "Kubernetes cluster is accessible"
    
    # Check if running in the correct directory
    if [[ ! -f "k8s/github-runners/namespace.yaml" ]]; then
        log_error "Please run this script from the repository root directory"
        exit 1
    fi
    log_success "Running from correct directory"
}

install_runner_controller() {
    log_info "Installing GitHub Actions Runner Controller..."
    
    # Check if already installed
    if kubectl get deployment -n actions-runner-system controller-manager &> /dev/null; then
        log_warning "GitHub Actions Runner Controller is already installed"
        return 0
    fi
    
    # Install controller
    kubectl apply -f "https://github.com/actions/actions-runner-controller/releases/latest/download/actions-runner-controller.yaml"
    
    # Wait for controller to be ready
    log_info "Waiting for controller to be ready..."
    kubectl wait --for=condition=Available deployment/controller-manager -n actions-runner-system --timeout=300s
    
    log_success "GitHub Actions Runner Controller installed successfully"
}

create_github_secret() {
    local github_token="$1"
    
    if [[ -z "$github_token" ]]; then
        log_error "GitHub token is required"
        exit 1
    fi
    
    log_info "Creating GitHub secret..."
    
    # Create namespace if it doesn't exist
    kubectl create namespace "$NAMESPACE" --dry-run=client -o yaml | kubectl apply -f -
    
    # Delete existing secret if it exists
    kubectl delete secret "$SECRET_NAME" -n "$NAMESPACE" --ignore-not-found=true
    
    # Create secret
    kubectl create secret generic "$SECRET_NAME" \
        --namespace="$NAMESPACE" \
        --from-literal=GITHUB_TOKEN="$github_token"
    
    log_success "GitHub secret created successfully"
}

deploy_runners() {
    local environment="$1"
    
    log_info "Deploying GitHub Actions runners..."
    
    case "$environment" in
        "dev"|"development")
            log_info "Deploying to development environment..."
            kubectl apply -f argocd/runners-application-dev.yaml
            ;;
        "prod"|"production")
            log_info "Deploying to production environment..."
            kubectl apply -f argocd/runners-application-prod.yaml
            ;;
        "both")
            log_info "Deploying to both environments..."
            kubectl apply -f argocd/runners-application-dev.yaml
            kubectl apply -f argocd/runners-application-prod.yaml
            ;;
        "manual")
            log_info "Deploying manually without ArgoCD..."
            kubectl apply -f k8s/github-runners/
            kubectl apply -f k8s/monitoring/
            ;;
        *)
            log_error "Invalid environment: $environment"
            log_info "Valid options: dev, prod, both, manual"
            exit 1
            ;;
    esac
    
    log_success "Deployment manifests applied successfully"
}

verify_deployment() {
    local environment="$1"
    local target_namespace="$NAMESPACE"
    
    if [[ "$environment" == "dev" || "$environment" == "development" ]]; then
        target_namespace="github-runners-dev"
    fi
    
    log_info "Verifying deployment in namespace: $target_namespace..."
    
    # Wait for pods to be ready
    log_info "Waiting for runner pods to be ready..."
    
    # Check if namespace exists
    if ! kubectl get namespace "$target_namespace" &> /dev/null; then
        log_warning "Namespace $target_namespace does not exist yet. It may take a few moments for ArgoCD to create it."
        return 0
    fi
    
    # Wait for RunnerDeployment
    local timeout=300
    local elapsed=0
    while [[ $elapsed -lt $timeout ]]; do
        if kubectl get runnerdeployment -n "$target_namespace" &> /dev/null; then
            log_success "RunnerDeployment found"
            break
        fi
        echo -n "."
        sleep 5
        elapsed=$((elapsed + 5))
    done
    
    if [[ $elapsed -ge $timeout ]]; then
        log_warning "RunnerDeployment not found within timeout. Check ArgoCD sync status."
        return 0
    fi
    
    # Check pod status
    kubectl get pods -n "$target_namespace" || log_warning "No pods found yet"
    
    log_success "Deployment verification completed"
}

show_status() {
    log_info "Current status of GitHub Actions runners:"
    
    echo
    echo "=== Namespaces ==="
    kubectl get namespaces | grep -E "(github-runners|actions-runner-system)" || log_warning "No runner namespaces found"
    
    echo
    echo "=== Runner Controller ==="
    kubectl get deployment -n actions-runner-system controller-manager || log_warning "Runner controller not found"
    
    echo
    echo "=== Runner Deployments ==="
    kubectl get runnerdeployment -A || log_warning "No runner deployments found"
    
    echo
    echo "=== Runner Pods ==="
    kubectl get pods -l app.kubernetes.io/name=mcp-server-dotnet-runners -A || log_warning "No runner pods found"
    
    echo
    echo "=== ArgoCD Applications ==="
    kubectl get application -n argocd | grep runners || log_warning "No runner applications found"
    
    echo
    echo "=== Secrets ==="
    kubectl get secret "$SECRET_NAME" -n "$NAMESPACE" || log_warning "GitHub secret not found"
}

cleanup() {
    log_warning "This will remove all GitHub Actions runners and related resources!"
    read -p "Are you sure? (y/N): " -n 1 -r
    echo
    
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        log_info "Cleanup cancelled"
        return 0
    fi
    
    log_info "Cleaning up GitHub Actions runners..."
    
    # Remove ArgoCD applications
    kubectl delete application github-runners-dev -n argocd --ignore-not-found=true
    kubectl delete application github-runners-prod -n argocd --ignore-not-found=true
    kubectl delete application github-runners -n argocd --ignore-not-found=true
    
    # Remove manual deployments
    kubectl delete -f k8s/github-runners/ --ignore-not-found=true
    kubectl delete -f k8s/monitoring/ --ignore-not-found=true
    
    # Remove namespaces
    kubectl delete namespace "$NAMESPACE" --ignore-not-found=true
    kubectl delete namespace "github-runners-dev" --ignore-not-found=true
    
    log_success "Cleanup completed"
}

show_help() {
    cat << EOF
GitHub Actions Self-Hosted Runners Setup Script

Usage: $0 [COMMAND] [OPTIONS]

Commands:
    install-controller                   Install GitHub Actions Runner Controller
    create-secret <github-token>         Create GitHub secret with PAT
    deploy <environment>                 Deploy runners (dev/prod/both/manual)
    verify <environment>                 Verify deployment (dev/prod)
    status                              Show current status
    cleanup                             Remove all runners and resources
    help                                Show this help message

Examples:
    $0 install-controller
    $0 create-secret ghp_1234567890abcdef
    $0 deploy prod
    $0 verify prod
    $0 status
    $0 cleanup

Environment options:
    dev, development     Deploy to development environment
    prod, production     Deploy to production environment  
    both                Deploy to both environments
    manual              Deploy manually without ArgoCD

Prerequisites:
    - kubectl installed and configured
    - Kubernetes cluster access
    - GitHub Personal Access Token with repo, workflow, admin:repo_hook permissions
    - ArgoCD installed (for non-manual deployments)

EOF
}

# Main script logic
main() {
    local command="${1:-help}"
    
    case "$command" in
        "install-controller")
            check_prerequisites
            install_runner_controller
            ;;
        "create-secret")
            check_prerequisites
            create_github_secret "$2"
            ;;
        "deploy")
            check_prerequisites
            deploy_runners "$2"
            ;;
        "verify")
            check_prerequisites
            verify_deployment "$2"
            ;;
        "status")
            check_prerequisites
            show_status
            ;;
        "cleanup")
            check_prerequisites
            cleanup
            ;;
        "help"|"-h"|"--help")
            show_help
            ;;
        *)
            log_error "Unknown command: $command"
            show_help
            exit 1
            ;;
    esac
}

# Run main function with all arguments
main "$@"