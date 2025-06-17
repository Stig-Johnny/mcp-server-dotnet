# Frontend-API Integration Guide

This document describes the integration between the React frontend and the MCP API through the Backend-for-Frontend (BFF) service.

## Architecture Overview

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│                 │    │                 │    │                 │
│  React Frontend │───▶│  BFF Service    │───▶│   MCP API       │
│                 │    │                 │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘
        │                      │                       │
        │                      │                       │
        ▼                      ▼                       ▼
  - TypeScript types     - API proxy endpoints   - MCP protocol
  - Error handling       - Authentication        - Tools & resources
  - API service client   - Configuration         - Business logic
```

## Components

### 1. React Frontend

**Location**: `src/Presentation/McpServer.Bff/frontend/`

**Key Files**:
- `src/types.ts` - TypeScript interfaces for API data
- `src/apiService.ts` - Service for making API calls
- `src/App.tsx` - Main component with data fetching and display

**Features**:
- Displays assets, MCP tools, and resources
- Handles authentication automatically
- Graceful error handling for API failures
- Responsive design with grid layouts
- Loading states and error messages

### 2. BFF Service

**Location**: `src/Presentation/McpServer.Bff/`

**Key Components**:
- `Services/McpApiService.cs` - HTTP client service for MCP API calls
- `Middleware/ApiKeyAuthenticationMiddleware.cs` - Authentication middleware
- `Models/McpModels.cs` - DTOs for MCP entities
- `Configuration/McpApiConfiguration.cs` - Configuration settings
- `Program.cs` - Service registration and endpoint configuration

**Endpoints**:
- `GET /api/assets` - Sample assets (no auth required)
- `GET /api/mcp/tools` - Proxy to MCP tools (auth required)
- `GET /api/mcp/resources` - Proxy to MCP resources (auth required)
- `GET /api/mcp/resources/content` - Proxy to resource content (auth required)
- `POST /api/mcp/tools/{toolName}/execute` - Proxy to tool execution (auth required)

### 3. MCP API

**Location**: `src/Presentation/McpServer.Api/`

**Endpoints**:
- `GET /api/mcp/tools` - Direct MCP tools access
- `GET /api/mcp/resources` - Direct MCP resources access
- `GET /api/mcp/resources/content` - Direct resource content access
- `POST /api/mcp/tools/{toolName}/execute` - Direct tool execution

## Authentication Flow

### API Key Authentication

The BFF service uses API key authentication for MCP endpoints:

1. **Frontend Request**: React app makes request to BFF endpoint
2. **Authentication**: BFF middleware checks for `X-API-Key` header
3. **Authorization**: Valid API key allows request to proceed
4. **Proxy**: BFF forwards request to MCP API
5. **Response**: MCP API response is returned to frontend

### Authentication Middleware

```csharp
public class ApiKeyAuthenticationMiddleware
{
    // Validates X-API-Key header for /api/mcp/* endpoints
    // Allows public access to assets, health checks, and React app
    // Returns 401 Unauthorized for invalid/missing keys
}
```

### Frontend API Service

```typescript
export class ApiService {
  private static readonly API_KEY = 'dev-api-key-123';
  
  // Automatically adds X-API-Key header for MCP endpoints
  // Handles authentication errors gracefully
  // Provides typed response handling
}
```

## Error Handling

### Frontend Error Handling

The React app implements comprehensive error handling:

```typescript
// Concurrent API calls with individual error handling
const [assetsData, toolsData, resourcesData] = await Promise.allSettled([
  ApiService.getAssets(),
  ApiService.getMcpTools(),
  ApiService.getMcpResources()
]);

// Graceful degradation - show available data even if some calls fail
// User-friendly error messages
// Retry mechanisms could be added
```

### BFF Error Handling

The BFF service handles errors at multiple levels:

```csharp
// HTTP client errors (connection failures, timeouts)
// Authentication errors (401 responses)
// MCP API errors (forwarded to frontend)
// Logging for debugging and monitoring
```

### Error Scenarios

1. **MCP API Unavailable**: BFF returns 500, frontend shows partial data
2. **Authentication Failure**: BFF returns 401, frontend shows auth error
3. **Network Issues**: Frontend shows connection error message
4. **Partial Failures**: Frontend shows available data, logs errors

## Data Flow

### 1. Application Startup

```
Frontend Load → Fetch Assets, Tools, Resources → Display Data
     ↓
  Concurrent API calls to BFF
     ↓
  BFF authenticates and proxies to MCP API
     ↓
  Responses aggregated and displayed
```

### 2. User Interaction

```
User Action → Frontend Event → API Call → BFF Proxy → MCP API
     ↓
  Response → BFF → Frontend → UI Update
```

## Configuration

### BFF Configuration

```json
{
  "McpApi": {
    "BaseUrl": "https://localhost:7001",
    "ApiKey": "dev-api-key-123"
  }
}
```

### Frontend Configuration

```typescript
// API service automatically uses configured endpoints
// No additional configuration needed
// API key is embedded (should be environment-specific in production)
```

## Security Considerations

### Current Implementation

- **API Key Authentication**: Simple shared key for BFF → MCP API
- **Public Frontend**: No user authentication required
- **HTTPS**: Enforced for secure communication
- **CORS**: Configured for cross-origin requests

### Production Recommendations

1. **User Authentication**: Implement proper user login/authentication
2. **JWT Tokens**: Replace API keys with JWT tokens
3. **Rate Limiting**: Implement rate limiting for API endpoints
4. **Environment Variables**: Store API keys in secure configuration
5. **Audit Logging**: Log all API access for security monitoring

## Testing

### Unit Tests

- **McpApiIntegrationTests**: Tests BFF authentication and proxy functionality
- **AssetsApiTests**: Tests original BFF functionality
- **Frontend Tests**: Could be added for React components

### Integration Testing

The integration tests verify:
- Authentication middleware works correctly
- Proxy endpoints forward requests properly
- Error handling returns appropriate status codes
- Public endpoints remain accessible

### Test Scenarios

1. **Authentication Tests**:
   - Valid API key → Success
   - Invalid API key → 401 Unauthorized
   - Missing API key → 401 Unauthorized

2. **Proxy Tests**:
   - BFF forwards requests to MCP API
   - Responses are returned correctly
   - Errors are handled appropriately

3. **Frontend Tests**:
   - API service handles errors gracefully
   - UI updates correctly based on API responses
   - Loading states work properly

## Development Guidelines

### Adding New Endpoints

1. **Add to MCP API**: Implement new endpoint in `McpController`
2. **Add to BFF**: Create proxy endpoint in `Program.cs`
3. **Update Frontend**: Add method to `ApiService` and update UI
4. **Add Tests**: Create integration tests for new functionality

### Modifying Authentication

1. **Update Middleware**: Modify `ApiKeyAuthenticationMiddleware`
2. **Update Frontend**: Modify `ApiService` authentication logic
3. **Update Configuration**: Add new configuration options
4. **Update Tests**: Ensure authentication tests pass

### Error Handling

1. **Consistent Error Responses**: Use standard HTTP status codes
2. **Logging**: Add appropriate logging for debugging
3. **User-Friendly Messages**: Provide clear error messages to users
4. **Graceful Degradation**: Show partial data when possible

## Monitoring and Observability

### Logging

The BFF service logs:
- Authentication attempts and failures
- API proxy requests and responses
- Error conditions and exceptions
- Performance metrics

### Health Checks

Both services provide health check endpoints:
- `/health` - Basic health status
- `/health/ready` - Readiness for traffic

### Metrics

Consider adding:
- Request/response times
- Error rates
- Authentication success/failure rates
- API usage patterns

## Future Enhancements

### Planned Improvements

1. **Real-time Updates**: WebSocket connections for live data
2. **Caching**: Redis caching for frequently accessed data
3. **User Management**: Proper user authentication and authorization
4. **Tool Interaction**: Frontend forms for executing MCP tools
5. **Resource Viewer**: Rich content display for MCP resources

### Architectural Improvements

1. **Service Mesh**: Consider Istio for production deployments
2. **API Gateway**: Dedicated API gateway for enterprise scenarios
3. **Event Sourcing**: Event-driven architecture for audit trails
4. **Microservices**: Split BFF into smaller, focused services

## Troubleshooting

### Common Issues

1. **Authentication Errors**:
   - Check API key configuration
   - Verify middleware registration
   - Check request headers

2. **Connection Errors**:
   - Verify MCP API is running
   - Check network connectivity
   - Review firewall settings

3. **CORS Issues**:
   - Configure CORS policy correctly
   - Check Origin headers
   - Verify preflight requests

### Debugging Steps

1. **Check Logs**: Review application logs for errors
2. **Network Tools**: Use browser dev tools to inspect requests
3. **Health Checks**: Verify service health endpoints
4. **Configuration**: Confirm all configuration values are correct

## API Reference

### BFF Endpoints

#### GET /api/assets
- **Description**: Get sample assets
- **Authentication**: None required
- **Response**: Array of asset objects

#### GET /api/mcp/tools
- **Description**: Get available MCP tools
- **Authentication**: X-API-Key header required
- **Response**: Array of tool objects

#### GET /api/mcp/resources
- **Description**: Get available MCP resources
- **Authentication**: X-API-Key header required
- **Response**: Array of resource objects

#### GET /api/mcp/resources/content
- **Description**: Get resource content
- **Authentication**: X-API-Key header required
- **Parameters**: `uri` (query parameter)
- **Response**: Resource content object

#### POST /api/mcp/tools/{toolName}/execute
- **Description**: Execute MCP tool
- **Authentication**: X-API-Key header required
- **Parameters**: `toolName` (path), request body with tool parameters
- **Response**: Tool execution result object

### TypeScript Types

```typescript
interface Asset {
  id: number;
  name: string;
  type: string;
  createdAt: string;
}

interface McpTool {
  name: string;
  description: string;
  parameters: Record<string, any>;
  createdAt: string;
}

interface McpResource {
  uri: string;
  name: string;
  description: string;
  mimeType: string;
  metadata: Record<string, any>;
}

interface McpToolResult {
  toolName: string;
  success: boolean;
  result?: any;
  errorMessage?: string;
  executedAt: string;
}
```

This integration provides a complete solution for frontend-API communication with proper authentication, error handling, and extensibility for future enhancements.