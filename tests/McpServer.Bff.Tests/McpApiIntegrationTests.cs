using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using System.Net;
using McpServer.Bff.Models;

namespace McpServer.Bff.Tests;

public class McpApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private const string ApiKey = "dev-api-key-123";

    public McpApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetMcpTools_WithoutApiKey_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/mcp/tools");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMcpTools_WithInvalidApiKey_ShouldReturnUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("X-API-Key", "invalid-key");

        // Act
        var response = await _client.GetAsync("/api/mcp/tools");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMcpTools_WithValidApiKey_ShouldReturnServiceUnavailable()
    {
        // Note: This will fail because the actual MCP API is not running, 
        // but it tests that authentication is working
        
        // Arrange
        _client.DefaultRequestHeaders.Add("X-API-Key", ApiKey);

        // Act
        var response = await _client.GetAsync("/api/mcp/tools");

        // Assert
        // We expect a 500 error because the MCP API is not available,
        // but this proves authentication passed
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task GetMcpResources_WithoutApiKey_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/mcp/resources");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMcpResources_WithValidApiKey_ShouldReturnServiceUnavailable()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("X-API-Key", ApiKey);

        // Act
        var response = await _client.GetAsync("/api/mcp/resources");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task GetAssets_ShouldStillWorkWithoutApiKey()
    {
        // Act - Assets endpoint should not require API key
        var response = await _client.GetAsync("/api/assets");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json; charset=utf-8", 
            response.Content.Headers.ContentType?.ToString());
    }

    [Fact]
    public async Task HealthCheck_ShouldWorkWithoutApiKey()
    {
        // Act - Health checks should not require API key
        var response = await _client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
    }
}