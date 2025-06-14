using Microsoft.AspNetCore.Mvc;
using McpServer.Application.Services;
using McpServer.Domain.Entities;

namespace McpServer.Api.Controllers;

/// <summary>
/// MCP API Controller for handling Model Context Protocol requests
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class McpController : ControllerBase
{
    private readonly McpApplicationService _mcpService;
    private readonly ILogger<McpController> _logger;

    public McpController(McpApplicationService mcpService, ILogger<McpController> logger)
    {
        _mcpService = mcpService ?? throw new ArgumentNullException(nameof(mcpService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all available MCP tools
    /// </summary>
    [HttpGet("tools")]
    public async Task<ActionResult<IEnumerable<McpTool>>> GetToolsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var tools = await _mcpService.GetAvailableToolsAsync(cancellationToken);
            return Ok(tools);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tools");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Execute an MCP tool
    /// </summary>
    [HttpPost("tools/{toolName}/execute")]
    public async Task<ActionResult<McpToolResult>> ExecuteToolAsync(
        string toolName,
        [FromBody] Dictionary<string, object>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(toolName))
                return BadRequest("Tool name is required");

            var result = await _mcpService.ExecuteToolAsync(toolName, parameters ?? new Dictionary<string, object>(), cancellationToken);
            
            if (result.Success)
                return Ok(result);
            else
                return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool: {ToolName}", toolName);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get all available MCP resources
    /// </summary>
    [HttpGet("resources")]
    public async Task<ActionResult<IEnumerable<McpResource>>> GetResourcesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var resources = await _mcpService.GetResourcesAsync(cancellationToken);
            return Ok(resources);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving resources");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get content of a specific MCP resource
    /// </summary>
    [HttpGet("resources/content")]
    public async Task<ActionResult<string>> GetResourceContentAsync(
        [FromQuery] string uri,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(uri))
                return BadRequest("URI is required");

            var content = await _mcpService.GetResourceContentAsync(uri, cancellationToken);
            return Ok(new { uri, content });
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving resource content: {Uri}", uri);
            return StatusCode(500, "Internal server error");
        }
    }
}