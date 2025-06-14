var builder = WebApplication.CreateBuilder(args);

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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MCP Server BFF API V1");
    });
}

app.UseHttpsRedirection();

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

app.Run();

// Make the Program class accessible to tests
public partial class Program { }