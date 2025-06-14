using McpServer.Application.Services;
using McpServer.Domain.Interfaces;
using McpServer.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add MCP services with correct lifetimes
builder.Services.AddSingleton<IMcpToolExecutor, BasicMcpToolExecutor>();
builder.Services.AddSingleton<IMcpResourceProvider, BasicMcpResourceProvider>();
builder.Services.AddScoped<McpApplicationService>();
builder.Services.AddHostedService<BasicMcpServer>();
builder.Services.AddSingleton<IMcpServer>(provider => 
{
    // Get the hosted service instance
    var hostingService = provider.GetServices<IHostedService>()
        .OfType<BasicMcpServer>()
        .First();
    return hostingService;
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "MCP Server API",
        Version = "v1",
        Description = "Model Context Protocol Server API"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MCP Server API V1");
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
