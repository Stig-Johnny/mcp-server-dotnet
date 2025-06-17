using McpServer.Bff.Configuration;
using McpServer.Bff.Services;
using McpServer.Bff.Models;
using McpServer.Bff.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add configuration
builder.Services.Configure<McpApiConfiguration>(
    builder.Configuration.GetSection(McpApiConfiguration.SectionName));

// Add HTTP client for MCP API
builder.Services.AddHttpClient<IMcpApiService, McpApiService>();

// Add health checks
builder.Services.AddHealthChecks();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "MCP Server BFF API",
        Version = "v1",
        Description = "Backend-for-Frontend API for MCP Server"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MCP Server BFF API V1");
});

app.UseHttpsRedirection();

// Add API key authentication for MCP endpoints
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();

// Serve static files from the React build output
app.UseStaticFiles();
app.UseDefaultFiles();

// Map health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");

// Assets endpoint - mocked for now
app.MapGet("/api/assets", () =>
{
    var assets = new[]
    {
        new { Id = 1, Name = "Sample Asset 1", Type = "Document", CreatedAt = DateTime.UtcNow.AddDays(-5) },
        new { Id = 2, Name = "Sample Asset 2", Type = "Image", CreatedAt = DateTime.UtcNow.AddDays(-3) },
        new { Id = 3, Name = "Sample Asset 3", Type = "Video", CreatedAt = DateTime.UtcNow.AddDays(-1) }
    };
    
    return Results.Ok(assets);
})
.WithName("GetAssets")
.WithOpenApi()
.Produces<object[]>(StatusCodes.Status200OK);

// MCP API proxy endpoints
app.MapGet("/api/mcp/tools", async (IMcpApiService mcpApiService) =>
{
    var tools = await mcpApiService.GetAsync<McpTool[]>("/api/mcp/tools");
    return tools != null ? Results.Ok(tools) : Results.Problem("Failed to fetch tools from MCP API");
})
.WithName("GetMcpTools")
.WithOpenApi()
.Produces<McpTool[]>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status500InternalServerError);

app.MapGet("/api/mcp/resources", async (IMcpApiService mcpApiService) =>
{
    var resources = await mcpApiService.GetAsync<McpResource[]>("/api/mcp/resources");
    return resources != null ? Results.Ok(resources) : Results.Problem("Failed to fetch resources from MCP API");
})
.WithName("GetMcpResources")
.WithOpenApi()
.Produces<McpResource[]>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status500InternalServerError);

app.MapGet("/api/mcp/resources/content", async (string uri, IMcpApiService mcpApiService) =>
{
    if (string.IsNullOrWhiteSpace(uri))
        return Results.BadRequest("URI parameter is required");
        
    var content = await mcpApiService.GetAsync<ResourceContent>($"/api/mcp/resources/content?uri={Uri.EscapeDataString(uri)}");
    return content != null ? Results.Ok(content) : Results.Problem("Failed to fetch resource content from MCP API");
})
.WithName("GetMcpResourceContent")
.WithOpenApi()
.Produces<ResourceContent>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status400BadRequest)
.ProducesProblem(StatusCodes.Status500InternalServerError);

app.MapPost("/api/mcp/tools/{toolName}/execute", async (string toolName, Dictionary<string, object>? parameters, IMcpApiService mcpApiService) =>
{
    if (string.IsNullOrWhiteSpace(toolName))
        return Results.BadRequest("Tool name is required");
        
    var result = await mcpApiService.PostAsync<McpToolResult>($"/api/mcp/tools/{Uri.EscapeDataString(toolName)}/execute", parameters);
    return result != null ? Results.Ok(result) : Results.Problem("Failed to execute tool via MCP API");
})
.WithName("ExecuteMcpTool")
.WithOpenApi()
.Produces<McpToolResult>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status400BadRequest)
.ProducesProblem(StatusCodes.Status500InternalServerError);

// Fallback to serve React app for client-side routing
app.MapFallback(async context =>
{
    context.Response.ContentType = "text/html";
    var indexPath = Path.Combine(app.Environment.WebRootPath, "index.html");
    if (File.Exists(indexPath))
    {
        await context.Response.SendFileAsync(indexPath);
    }
    else
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync("React app not found. Make sure to build the frontend first.");
    }
});

app.Run();

// Make the Program class accessible to tests
public partial class Program { }