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
- **Swagger UI**: `https://localhost:7001/swagger`
- **Health Check**: `https://localhost:7001/health`
- **Aspire Dashboard**: `https://localhost:15888` (when running with Aspire Host)

## API Endpoints

### MCP Tools

- `GET /api/mcp/tools` - Get all available MCP tools
- `POST /api/mcp/tools/{toolName}/execute` - Execute a specific tool

Example tools:
- `echo` - Echoes back a message
- `time` - Returns current time information
- `calculate` - Performs basic arithmetic calculations

### MCP Resources

- `GET /api/mcp/resources` - Get all available MCP resources
- `GET /api/mcp/resources/content?uri={uri}` - Get content of a specific resource

## Usage Examples

### Execute Echo Tool

```bash
curl -X POST "https://localhost:7001/api/mcp/tools/echo/execute" \
  -H "Content-Type: application/json" \
  -d '{"message": "Hello, MCP!"}'
```

### Get Current Time

```bash
curl -X POST "https://localhost:7001/api/mcp/tools/time/execute" \
  -H "Content-Type: application/json" \
  -d '{}'
```

### Perform Calculation

```bash
curl -X POST "https://localhost:7001/api/mcp/tools/calculate/execute" \
  -H "Content-Type: application/json" \
  -d '{"expression": "2+2"}'
```

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
