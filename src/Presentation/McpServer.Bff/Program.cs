using McpServer.Application.Services;
using McpServer.Domain.Interfaces;
using McpServer.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add health checks
builder.Services.AddHealthChecks();

// Add MCP services
builder.Services.AddSingleton<IMcpToolExecutor, BasicMcpToolExecutor>();
builder.Services.AddSingleton<IMcpResourceProvider, BasicMcpResourceProvider>();
builder.Services.AddScoped<McpApplicationService>();

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

// MCP Tools endpoints
app.MapGet("/api/mcp/tools", async (McpApplicationService mcpService, CancellationToken cancellationToken) =>
{
    try
    {
        var tools = await mcpService.GetAvailableToolsAsync(cancellationToken);
        return Results.Ok(tools);
    }
    catch
    {
        return Results.Problem("Failed to retrieve tools", statusCode: 500);
    }
})
.WithName("GetMcpTools")
.WithOpenApi()
.Produces<object[]>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status500InternalServerError);

app.MapPost("/api/mcp/tools/{toolName}/execute", async (
    string toolName,
    McpApplicationService mcpService,
    Dictionary<string, object>? parameters,
    CancellationToken cancellationToken) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(toolName))
            return Results.BadRequest("Tool name is required");

        var result = await mcpService.ExecuteToolAsync(toolName, parameters ?? new Dictionary<string, object>(), cancellationToken);
        return Results.Ok(result);
    }
    catch
    {
        return Results.Problem("Failed to execute tool", statusCode: 500);
    }
})
.WithName("ExecuteMcpTool")
.WithOpenApi()
.Produces<object>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status400BadRequest)
.ProducesProblem(StatusCodes.Status500InternalServerError);

// MCP Resources endpoints
app.MapGet("/api/mcp/resources", async (McpApplicationService mcpService, CancellationToken cancellationToken) =>
{
    try
    {
        var resources = await mcpService.GetResourcesAsync(cancellationToken);
        return Results.Ok(resources);
    }
    catch
    {
        return Results.Problem("Failed to retrieve resources", statusCode: 500);
    }
})
.WithName("GetMcpResources")
.WithOpenApi()
.Produces<object[]>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status500InternalServerError);

app.MapGet("/api/mcp/resources/content", async (
    string uri,
    McpApplicationService mcpService,
    CancellationToken cancellationToken) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(uri))
            return Results.BadRequest("URI is required");

        var content = await mcpService.GetResourceContentAsync(uri, cancellationToken);
        return Results.Ok(new { uri, content });
    }
    catch (ArgumentException ex)
    {
        return Results.NotFound(ex.Message);
    }
    catch
    {
        return Results.Problem("Failed to retrieve resource content", statusCode: 500);
    }
})
.WithName("GetMcpResourceContent")
.WithOpenApi()
.Produces<object>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status400BadRequest)
.ProducesProblem(StatusCodes.Status404NotFound)
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