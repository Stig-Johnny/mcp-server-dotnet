using McpServer.Domain.Entities;
using McpServer.Domain.Interfaces;

namespace McpServer.Application.Services;

/// <summary>
/// Application service for managing MCP operations
/// </summary>
public class McpApplicationService
{
    private readonly IMcpToolExecutor _toolExecutor;
    private readonly IMcpResourceProvider _resourceProvider;

    public McpApplicationService(IMcpToolExecutor toolExecutor, IMcpResourceProvider resourceProvider)
    {
        _toolExecutor = toolExecutor ?? throw new ArgumentNullException(nameof(toolExecutor));
        _resourceProvider = resourceProvider ?? throw new ArgumentNullException(nameof(resourceProvider));
    }

    public async Task<McpToolResult> ExecuteToolAsync(string toolName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            throw new ArgumentException("Tool name cannot be null or empty", nameof(toolName));

        return await _toolExecutor.ExecuteAsync(toolName, parameters ?? new Dictionary<string, object>(), cancellationToken);
    }

    public async Task<IEnumerable<McpTool>> GetAvailableToolsAsync(CancellationToken cancellationToken = default)
    {
        return await _toolExecutor.GetAvailableToolsAsync(cancellationToken);
    }

    public async Task<IEnumerable<McpResource>> GetResourcesAsync(CancellationToken cancellationToken = default)
    {
        return await _resourceProvider.GetResourcesAsync(cancellationToken);
    }

    public async Task<string> GetResourceContentAsync(string uri, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(uri))
            throw new ArgumentException("URI cannot be null or empty", nameof(uri));

        return await _resourceProvider.GetResourceContentAsync(uri, cancellationToken);
    }
}
