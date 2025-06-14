using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;

namespace McpServer.Bff.Tests;

public class AssetsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AssetsApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAssets_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/assets");

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        Assert.Equal("application/json; charset=utf-8", 
            response.Content.Headers.ContentType?.ToString());
    }

    [Fact]
    public async Task GetAssets_ShouldReturnExpectedData()
    {
        // Act
        var response = await _client.GetAsync("/api/assets");
        var assets = await response.Content.ReadFromJsonAsync<object[]>();

        // Assert
        Assert.NotNull(assets);
        Assert.Equal(3, assets.Length);
    }

    [Fact]
    public async Task GetAssets_ShouldReturnAssetsWithCorrectStructure()
    {
        // Act
        var response = await _client.GetAsync("/api/assets");
        var jsonString = await response.Content.ReadAsStringAsync();

        // Assert - JSON serialization uses camelCase by default
        Assert.Contains("\"id\":", jsonString);
        Assert.Contains("\"name\":", jsonString); 
        Assert.Contains("\"type\":", jsonString);
        Assert.Contains("\"createdAt\":", jsonString);
        Assert.Contains("Sample Asset", jsonString);
    }
}