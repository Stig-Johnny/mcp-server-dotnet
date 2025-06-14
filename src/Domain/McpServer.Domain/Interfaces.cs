using McpServer.Domain.Entities;

namespace McpServer.Domain.Interfaces;

/// <summary>
/// Interface for MCP tool execution
/// </summary>
public interface IMcpToolExecutor
{
    Task<McpToolResult> ExecuteAsync(string toolName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);
    Task<IEnumerable<McpTool>> GetAvailableToolsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for MCP resource management
/// </summary>
public interface IMcpResourceProvider
{
    Task<McpResource?> GetResourceAsync(string uri, CancellationToken cancellationToken = default);
    Task<IEnumerable<McpResource>> GetResourcesAsync(CancellationToken cancellationToken = default);
    Task<string> GetResourceContentAsync(string uri, CancellationToken cancellationToken = default);
}

/// <summary>
/// Main MCP server interface
/// </summary>
public interface IMcpServer
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    bool IsRunning { get; }
}