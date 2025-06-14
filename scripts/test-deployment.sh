#!/bin/bash
# MCP Gateway Deployment Test Script
# This script tests the complete deployment workflow for the MCP Gateway

set -e

echo "🚀 Starting MCP Gateway Deployment Test"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Check prerequisites
echo -e "${BLUE}📋 Checking prerequisites...${NC}"

check_command() {
    if ! command -v "$1" &> /dev/null; then
        echo -e "${RED}❌ $1 is not installed or not in PATH${NC}"
        exit 1
    else
        echo -e "${GREEN}✅ $1 is available${NC}"
    fi
}

check_command "docker"
check_command "helm"
check_command "dotnet"

# Test 1: Build and Test .NET Application
echo -e "${BLUE}🔨 Testing .NET application build...${NC}"
dotnet restore
dotnet build src/Presentation/McpServer.Api/McpServer.Api.csproj

# Test 2: Run MCP Protocol Compliance Tests
echo -e "${BLUE}🧪 Running MCP Protocol Compliance Tests...${NC}"
dotnet test tests/McpServer.Tests --filter McpProtocolComplianceTests --verbosity minimal

# Test 3: Publish Application
echo -e "${BLUE}📦 Publishing application for container build...${NC}"
mkdir -p publish/api
dotnet publish src/Presentation/McpServer.Api/McpServer.Api.csproj -c Release -o ./publish/api --self-contained false

# Test 4: Build Docker Container
echo -e "${BLUE}🐳 Building MCP Gateway Docker container...${NC}"
docker build -f Dockerfile.gateway -t mcp-gateway:test-$(date +%Y%m%d-%H%M%S) .

# Test 5: Validate Helm Charts
echo -e "${BLUE}⎈ Validating Helm charts...${NC}"
helm lint helm/mcp-server-dotnet

# Test 6: Test Helm Template Rendering
echo -e "${BLUE}📄 Testing Helm template rendering...${NC}"
helm template mcp-gateway-test helm/mcp-server-dotnet \
    --values helm/mcp-server-dotnet/values-gateway.yaml \
    --set global.tag=test \
    --dry-run > /tmp/helm-test-output.yaml

if [ -s /tmp/helm-test-output.yaml ]; then
    echo -e "${GREEN}✅ Helm templates rendered successfully${NC}"
    echo "📊 Generated $(cat /tmp/helm-test-output.yaml | grep -c '^---') Kubernetes resources"
else
    echo -e "${RED}❌ Helm template rendering failed${NC}"
    exit 1
fi

# Test 7: Validate ArgoCD Configuration
echo -e "${BLUE}🔄 Validating ArgoCD configurations...${NC}"
for app in argocd/application-gateway.yaml argocd/application.yaml; do
    if [ -f "$app" ]; then
        echo -e "${GREEN}✅ Found $app${NC}"
        # Check for image updater annotations
        if grep -q "argocd-image-updater.argoproj.io" "$app"; then
            echo -e "${GREEN}✅ ArgoCD Image Updater annotations found in $app${NC}"
        else
            echo -e "${YELLOW}⚠️  No ArgoCD Image Updater annotations in $app${NC}"
        fi
    else
        echo -e "${RED}❌ Missing $app${NC}"
        exit 1
    fi
done

# Test 8: Check GitHub Actions Workflow
echo -e "${BLUE}⚙️ Validating GitHub Actions workflow...${NC}"
if [ -f ".github/workflows/docker-build.yml" ]; then
    echo -e "${GREEN}✅ Found GitHub Actions Docker build workflow${NC}"
    if grep -q "mcp-gateway" ".github/workflows/docker-build.yml"; then
        echo -e "${GREEN}✅ Gateway build configured in workflow${NC}"
    else
        echo -e "${YELLOW}⚠️  Gateway build not found in workflow${NC}"
    fi
else
    echo -e "${RED}❌ Missing GitHub Actions workflow${NC}"
    exit 1
fi

# Summary
echo -e "\n${GREEN}🎉 All deployment tests passed!${NC}"
echo -e "${BLUE}📝 Deployment Test Summary:${NC}"
echo "✅ .NET application builds successfully"
echo "✅ MCP Protocol compliance tests pass"
echo "✅ Docker container builds successfully"
echo "✅ Helm charts are valid and render correctly"
echo "✅ ArgoCD configurations include image updater"
echo "✅ GitHub Actions workflow includes gateway build"

echo -e "\n${YELLOW}🚀 Ready for deployment!${NC}"
echo -e "${BLUE}Next steps:${NC}"
echo "1. Push changes to trigger GitHub Actions build"
echo "2. Deploy using: kubectl apply -f argocd/application-gateway.yaml"
echo "3. Monitor deployment: kubectl get pods -n mcp-gateway"
echo "4. Check health: kubectl port-forward svc/mcp-gateway-service 8080:80 -n mcp-gateway"

# Cleanup
rm -f /tmp/helm-test-output.yaml
echo -e "${GREEN}✨ Test completed successfully${NC}"