using Microsoft.Extensions.Logging;
using Moq;
using McpServer.Infrastructure.Services;

namespace McpServer.Tests;

public class BasicMcpToolExecutorTests
{
    private readonly Mock<ILogger<BasicMcpToolExecutor>> _loggerMock;
    private readonly BasicMcpToolExecutor _executor;

    public BasicMcpToolExecutorTests()
    {
        _loggerMock = new Mock<ILogger<BasicMcpToolExecutor>>();
        _executor = new BasicMcpToolExecutor(_loggerMock.Object);
    }

    [Fact]
    public async Task GetAvailableToolsAsync_ShouldReturnTools()
    {
        // Act
        var tools = await _executor.GetAvailableToolsAsync();

        // Assert
        Assert.NotEmpty(tools);
        Assert.Contains(tools, t => t.Name == "echo");
        Assert.Contains(tools, t => t.Name == "time");
        Assert.Contains(tools, t => t.Name == "calculate");
    }

    [Fact]
    public async Task ExecuteAsync_EchoTool_ShouldReturnMessage()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { ["message"] = "Test message" };

        // Act
        var result = await _executor.ExecuteAsync("echo", parameters);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("echo", result.ToolName);
        Assert.NotNull(result.Result);
    }

    [Fact]
    public async Task ExecuteAsync_TimeTool_ShouldReturnTimeInfo()
    {
        // Act
        var result = await _executor.ExecuteAsync("time", new Dictionary<string, object>());

        // Assert
        Assert.True(result.Success);
        Assert.Equal("time", result.ToolName);
        Assert.NotNull(result.Result);
    }

    [Fact]
    public async Task ExecuteAsync_CalculateTool_ShouldPerformCalculation()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { ["expression"] = "2+2" };

        // Act
        var result = await _executor.ExecuteAsync("calculate", parameters);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("calculate", result.ToolName);
        Assert.NotNull(result.Result);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownTool_ShouldReturnError()
    {
        // Act
        var result = await _executor.ExecuteAsync("unknown", new Dictionary<string, object>());

        // Assert
        Assert.False(result.Success);
        Assert.Equal("unknown", result.ToolName);
        Assert.NotNull(result.ErrorMessage);
    }
}