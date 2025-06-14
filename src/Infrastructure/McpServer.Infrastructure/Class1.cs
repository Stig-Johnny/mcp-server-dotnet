using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using McpServer.Domain.Entities;
using McpServer.Domain.Interfaces;

namespace McpServer.Infrastructure.Services;

/// <summary>
/// Basic implementation of MCP tool executor
/// </summary>
public class BasicMcpToolExecutor : IMcpToolExecutor
{
    private readonly ILogger<BasicMcpToolExecutor> _logger;
    private readonly List<McpTool> _availableTools;

    public BasicMcpToolExecutor(ILogger<BasicMcpToolExecutor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _availableTools = InitializeTools();
    }

    public async Task<McpToolResult> ExecuteAsync(string toolName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing tool: {ToolName}", toolName);

        try
        {
            // Simple example implementation
            await Task.Delay(100, cancellationToken); // Simulate work

            var result = toolName.ToLowerInvariant() switch
            {
                "echo" => ExecuteEchoTool(parameters),
                "time" => ExecuteTimeTool(),
                "calculate" => ExecuteCalculateTool(parameters),
                _ => new McpToolResult
                {
                    ToolName = toolName,
                    Success = false,
                    ErrorMessage = $"Tool '{toolName}' not found"
                }
            };

            _logger.LogInformation("Tool execution completed: {ToolName}, Success: {Success}", toolName, result.Success);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool: {ToolName}", toolName);
            return new McpToolResult
            {
                ToolName = toolName,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public Task<IEnumerable<McpTool>> GetAvailableToolsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<McpTool>>(_availableTools);
    }

    private List<McpTool> InitializeTools()
    {
        return new List<McpTool>
        {
            new McpTool
            {
                Name = "echo",
                Description = "Echoes back the provided message",
                Parameters = new Dictionary<string, object>
                {
                    ["message"] = "The message to echo back"
                }
            },
            new McpTool
            {
                Name = "time",
                Description = "Returns the current time",
                Parameters = new Dictionary<string, object>()
            },
            new McpTool
            {
                Name = "calculate",
                Description = "Performs basic arithmetic calculations",
                Parameters = new Dictionary<string, object>
                {
                    ["expression"] = "Mathematical expression (e.g., '2+2')"
                }
            }
        };
    }

    private McpToolResult ExecuteEchoTool(Dictionary<string, object> parameters)
    {
        var message = parameters.GetValueOrDefault("message", "Hello, World!").ToString();
        return new McpToolResult
        {
            ToolName = "echo",
            Success = true,
            Result = new { message = message, timestamp = DateTime.UtcNow }
        };
    }

    private McpToolResult ExecuteTimeTool()
    {
        return new McpToolResult
        {
            ToolName = "time",
            Success = true,
            Result = new { 
                utc = DateTime.UtcNow,
                local = DateTime.Now,
                timezone = TimeZoneInfo.Local.Id
            }
        };
    }

    private McpToolResult ExecuteCalculateTool(Dictionary<string, object> parameters)
    {
        var expression = parameters.GetValueOrDefault("expression", "0").ToString() ?? "0";
        
        try
        {
            // Simple calculator for basic operations
            var result = EvaluateExpression(expression);
            return new McpToolResult
            {
                ToolName = "calculate",
                Success = true,
                Result = new { expression = expression, result = result }
            };
        }
        catch (Exception ex)
        {
            return new McpToolResult
            {
                ToolName = "calculate",
                Success = false,
                ErrorMessage = $"Invalid expression: {ex.Message}"
            };
        }
    }

    private double EvaluateExpression(string expression)
    {
        // Very basic calculator - for demo purposes only
        // In a real implementation, you'd use a proper math parser
        expression = expression.Replace(" ", "");
        
        if (expression.Contains("+"))
        {
            var parts = expression.Split('+');
            return double.Parse(parts[0]) + double.Parse(parts[1]);
        }
        if (expression.Contains("-"))
        {
            var parts = expression.Split('-');
            return double.Parse(parts[0]) - double.Parse(parts[1]);
        }
        if (expression.Contains("*"))
        {
            var parts = expression.Split('*');
            return double.Parse(parts[0]) * double.Parse(parts[1]);
        }
        if (expression.Contains("/"))
        {
            var parts = expression.Split('/');
            return double.Parse(parts[0]) / double.Parse(parts[1]);
        }
        
        return double.Parse(expression);
    }
}

/// <summary>
/// Basic implementation of MCP resource provider
/// </summary>
public class BasicMcpResourceProvider : IMcpResourceProvider
{
    private readonly ILogger<BasicMcpResourceProvider> _logger;
    private readonly List<McpResource> _resources;

    public BasicMcpResourceProvider(ILogger<BasicMcpResourceProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resources = InitializeResources();
    }

    public Task<McpResource?> GetResourceAsync(string uri, CancellationToken cancellationToken = default)
    {
        var resource = _resources.FirstOrDefault(r => r.Uri == uri);
        return Task.FromResult(resource);
    }

    public Task<IEnumerable<McpResource>> GetResourcesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<McpResource>>(_resources);
    }

    public async Task<string> GetResourceContentAsync(string uri, CancellationToken cancellationToken = default)
    {
        var resource = await GetResourceAsync(uri, cancellationToken);
        if (resource == null)
            throw new ArgumentException($"Resource not found: {uri}");

        // Simulate content retrieval
        return uri switch
        {
            "mcp://example/info" => "This is sample information from the MCP server.",
            "mcp://example/data" => """{"sample": "data", "timestamp": """ + DateTime.UtcNow.ToString("O") + """}""",
            _ => "Content not available"
        };
    }

    private List<McpResource> InitializeResources()
    {
        return new List<McpResource>
        {
            new McpResource
            {
                Uri = "mcp://example/info",
                Name = "Sample Information",
                Description = "Basic information resource",
                MimeType = "text/plain"
            },
            new McpResource
            {
                Uri = "mcp://example/data",
                Name = "Sample Data",
                Description = "JSON data resource",
                MimeType = "application/json"
            }
        };
    }
}

/// <summary>
/// Basic MCP server implementation
/// </summary>
public class BasicMcpServer : BackgroundService, IMcpServer
{
    private readonly ILogger<BasicMcpServer> _logger;
    private readonly IMcpToolExecutor _toolExecutor;
    private readonly IMcpResourceProvider _resourceProvider;

    public BasicMcpServer(
        ILogger<BasicMcpServer> logger,
        IMcpToolExecutor toolExecutor,
        IMcpResourceProvider resourceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _toolExecutor = toolExecutor ?? throw new ArgumentNullException(nameof(toolExecutor));
        _resourceProvider = resourceProvider ?? throw new ArgumentNullException(nameof(resourceProvider));
    }

    public bool IsRunning { get; private set; }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting MCP Server...");
        IsRunning = true;
        await base.StartAsync(cancellationToken);
        _logger.LogInformation("MCP Server started successfully");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping MCP Server...");
        IsRunning = false;
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("MCP Server stopped");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Main server loop - in a real implementation, this would handle MCP protocol messages
            await Task.Delay(5000, stoppingToken);
            _logger.LogDebug("MCP Server heartbeat");
        }
    }
}
