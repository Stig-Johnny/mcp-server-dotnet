var builder = WebApplication.CreateBuilder(args);

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