# MCP Server .NET

A .NET 8 implementation of the Model Context Protocol (MCP) server using Clean Architecture, Domain-Driven Design (DDD), and .NET Aspire for orchestration.

## Architecture

This solution follows Clean Architecture principles with clear separation of concerns:

```
├── src/
│   ├── Domain/                 # Core business logic and entities
│   ├── Application/            # Use cases and application services
│   ├── Infrastructure/         # External concerns and implementations
│   ├── Presentation/           # Web API controllers and presentation logic
│   └── Host/                   # Aspire orchestration host
```

## Features

- **Clean Architecture**: Clear separation between layers with dependency inversion
- **Domain-Driven Design**: Rich domain model with proper encapsulation
- **MCP Protocol Support**: Model Context Protocol implementation with tools and resources
- **ASP.NET Core Web API**: RESTful API for MCP operations
- **React Frontend**: Modern web UI with TypeScript for interacting with MCP services
- **Backend-for-Frontend (BFF)**: Dedicated API gateway for frontend integration
- **API Key Authentication**: Secure access to MCP endpoints
- **.NET Aspire**: Modern orchestration and observability
- **Docker Support**: Container-ready with optimized Dockerfile
- **Swagger/OpenAPI**: API documentation and testing interface

## Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/get-started) (optional, for containerization)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [JetBrains Rider](https://www.jetbrains.com/rider/) (recommended)
- [Kubernetes cluster with ArgoCD](docs/kubernetes-deployment.md) (optional, for production deployment)

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/Stig-Johnny/mcp-server-dotnet.git
cd mcp-server-dotnet
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Build the Solution

```bash
dotnet build
```

### 4. Run the Application

#### Option A: Run with Aspire Host (Recommended for Development)
```bash
dotnet run --project src/Host/McpServer.Host
```

#### Option B: Run Individual Services
```bash
# Run the API
dotnet run --project src/Presentation/McpServer.Api

# Run the BFF (in another terminal)
dotnet run --project src/Presentation/McpServer.Bff
```

#### Option C: Deploy to Kubernetes (Production)
```bash
# Quick deployment with ArgoCD
kubectl apply -f argocd/application.yaml

# Or manual deployment with Helm
helm install mcp-server ./helm/mcp-server-dotnet \
  --namespace mcp-server \
  --create-namespace
```

### 5. Access the Application

- **API Base URL**: `https://localhost:7001` or `http://localhost:5001`
- **BFF Base URL**: `https://localhost:7245` or `http://localhost:5245`
- **React Frontend**: `https://localhost:7245` (served by BFF)
- **Swagger UI**: `https://localhost:7001/swagger` (API) and `https://localhost:7245/swagger` (BFF)
- **Health Check**: `https://localhost:7001/health` and `https://localhost:7245/health`
- **Aspire Dashboard**: `https://localhost:15888` (when running with Aspire Host)

## API Endpoints

### MCP API (Direct Access)

- `GET /api/mcp/tools` - Get all available MCP tools
- `POST /api/mcp/tools/{toolName}/execute` - Execute a specific tool
- `GET /api/mcp/resources` - Get all available MCP resources
- `GET /api/mcp/resources/content?uri={uri}` - Get content of a specific resource

### BFF API (Frontend Integration)

- `GET /api/assets` - Get sample assets (demo data)
- `GET /api/mcp/tools` - Proxy to MCP API tools (requires API key)
- `POST /api/mcp/tools/{toolName}/execute` - Proxy to MCP API tool execution (requires API key)
- `GET /api/mcp/resources` - Proxy to MCP API resources (requires API key)
- `GET /api/mcp/resources/content?uri={uri}` - Proxy to MCP API resource content (requires API key)

**Note**: BFF MCP endpoints require the `X-API-Key` header with value `dev-api-key-123` for authentication.

### Frontend Features

The React frontend provides:
- **Assets Display**: Shows sample assets from the BFF
- **MCP Tools**: Lists and displays available MCP tools with their descriptions
- **MCP Resources**: Shows available MCP resources with their metadata
- **Error Handling**: Graceful handling of API errors and connection issues
- **Authentication**: Automatic API key inclusion for MCP endpoints

## Usage Examples

### Direct MCP API Access

#### Execute Echo Tool

```bash
curl -X POST "https://localhost:7001/api/mcp/tools/echo/execute" \
  -H "Content-Type: application/json" \
  -d '{"message": "Hello, MCP!"}'
```

#### Get Current Time

```bash
curl -X POST "https://localhost:7001/api/mcp/tools/time/execute" \
  -H "Content-Type: application/json" \
  -d '{}'
```

#### Perform Calculation

```bash
curl -X POST "https://localhost:7001/api/mcp/tools/calculate/execute" \
  -H "Content-Type: application/json" \
  -d '{"expression": "2+2"}'
```

### BFF API Access (Frontend Integration)

#### Get MCP Tools (with authentication)

```bash
curl -X GET "https://localhost:7245/api/mcp/tools" \
  -H "X-API-Key: dev-api-key-123"
```

#### Execute Tool via BFF

```bash
curl -X POST "https://localhost:7245/api/mcp/tools/echo/execute" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: dev-api-key-123" \
  -d '{"message": "Hello via BFF!"}'
```

#### Frontend Access

Simply navigate to `https://localhost:7245` in your browser to access the React frontend, which automatically:
- Fetches and displays assets, MCP tools, and resources
- Handles authentication with proper API keys
- Provides error handling for failed API calls

Example tools available:
- `echo` - Echoes back a message
- `time` - Returns current time information
- `calculate` - Performs basic arithmetic calculations

## Docker Support

### Build Docker Image

```bash
docker build -f src/Presentation/McpServer.Api/Dockerfile -t mcp-server-dotnet .
```

### Run Container

```bash
docker run -p 8080:80 mcp-server-dotnet
```

Access the API at `http://localhost:8080`

## Development

### Project Structure

- **McpServer.Domain**: Contains core entities, value objects, and domain interfaces
- **McpServer.Application**: Contains application services and use cases
- **McpServer.Infrastructure**: Contains concrete implementations of domain interfaces
- **McpServer.Api**: Contains Web API controllers and presentation logic
- **McpServer.Host**: Contains Aspire host for orchestration

### Core Principles

This project follows key software development principles:

- **KISS (Keep It Simple, Stupid)**: Favor simplicity and clarity in design
- **DRY (Don't Repeat Yourself)**: Avoid code duplication through abstraction
- **YAGNI (You Aren't Gonna Need It)**: Implement only what's necessary

### Adding New MCP Tools

1. Add tool definition in `BasicMcpToolExecutor.InitializeTools()`
2. Implement execution logic in `BasicMcpToolExecutor.ExecuteAsync()`
3. Update API documentation if needed

### Adding New MCP Resources

1. Add resource definition in `BasicMcpResourceProvider.InitializeResources()`
2. Implement content retrieval in `BasicMcpResourceProvider.GetResourceContentAsync()`

## Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Configuration

The application uses standard .NET configuration. Key settings can be configured via:

- `appsettings.json`
- Environment variables
- Command line arguments
- Azure Key Vault (in production)

### BFF Configuration

The Backend-for-Frontend service has additional configuration for MCP API integration:

```json
{
  "McpApi": {
    "BaseUrl": "https://localhost:7001",
    "ApiKey": "dev-api-key-123"
  }
}
```

Configuration options:
- `McpApi:BaseUrl` - URL of the MCP API service
- `McpApi:ApiKey` - API key for authenticating BFF requests to MCP endpoints

### Frontend Configuration

The React frontend automatically uses the configured API key for MCP endpoint authentication. In production, consider:
- Storing API keys securely (environment variables, key vault)
- Using different API keys per environment
- Implementing proper user authentication instead of shared API keys

## Deployment

### Local Development

Use the Aspire Host for the best development experience:

```bash
dotnet run --project src/Host/McpServer.Host
```

### Production

#### Kubernetes with ArgoCD (Recommended)

Deploy to Kubernetes using ArgoCD and Helm:

```bash
# Apply ArgoCD application
kubectl apply -f argocd/application.yaml

# Or deploy manually with Helm
helm install mcp-server ./helm/mcp-server-dotnet \
  --namespace mcp-server \
  --create-namespace \
  --set global.tag=main
```

For detailed Kubernetes deployment instructions, see [Kubernetes Deployment Guide](docs/kubernetes-deployment.md).

#### Docker Container

Deploy as a Docker container or directly to Azure App Service, AWS, or any .NET-compatible hosting platform.

```bash
# Build and run API
docker build -f src/Presentation/McpServer.Api/Dockerfile -t mcp-server-api .
docker run -p 8080:80 mcp-server-api

# Build and run BFF
docker build -f src/Presentation/McpServer.Bff/Dockerfile -t mcp-server-bff .
docker run -p 8081:80 mcp-server-bff
```

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Resources

- [Model Context Protocol Specification](https://github.com/anthropics/mcp)
- [Microsoft C# SDK for MCP](https://devblogs.microsoft.com/blog/microsoft-partners-with-anthropic-to-create-official-c-sdk-for-model-context-protocol)
- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Domain-Driven Design](https://martinfowler.com/bliki/DomainDrivenDesign.html)

## Support

For questions and support, please open an issue in the GitHub repository.
