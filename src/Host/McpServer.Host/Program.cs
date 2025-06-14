using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Add the MCP API project
builder.AddProject("mcp-api", "../../Presentation/McpServer.Api/McpServer.Api.csproj");

// Add the BFF project
builder.AddProject("mcp-bff", "../../Presentation/McpServer.Bff/McpServer.Bff.csproj");

builder.Build().Run();
