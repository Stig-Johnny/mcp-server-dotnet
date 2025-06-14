using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Add the MCP API project - simplified for now
builder.AddProject("mcp-api", "../Presentation/McpServer.Api/McpServer.Api.csproj");

builder.Build().Run();
