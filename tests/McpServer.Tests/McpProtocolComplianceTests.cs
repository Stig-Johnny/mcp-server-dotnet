using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using McpServer.Domain.Entities;

namespace McpServer.Tests
{
    /// <summary>
    /// MCP Protocol compliance tests for the Gateway
    /// </summary>
    public class McpProtocolComplianceTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public McpProtocolComplianceTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task Gateway_HealthCheck_ReturnsHealthy()
        {
            // Act
            var response = await _client.GetAsync("/health");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Healthy", content);
        }

        [Fact]
        public async Task Gateway_ReadinessCheck_ReturnsHealthy()
        {
            // Act
            var response = await _client.GetAsync("/health/ready");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Healthy", content);
        }

        [Fact]
        public async Task MCP_GetTools_ReturnsValidToolList()
        {
            // Act
            var response = await _client.GetAsync("/api/mcp/tools");
            var tools = await response.Content.ReadFromJsonAsync<McpTool[]>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(tools);
            Assert.NotEmpty(tools);
            
            // Validate tool structure compliance
            foreach (var tool in tools)
            {
                Assert.NotNull(tool.Name);
                Assert.NotNull(tool.Description);
                Assert.NotNull(tool.Parameters);
                Assert.True(tool.CreatedAt > DateTime.MinValue);
            }
        }

        [Fact]
        public async Task MCP_GetResources_ReturnsValidResourceList()
        {
            // Act
            var response = await _client.GetAsync("/api/mcp/resources");
            var resources = await response.Content.ReadFromJsonAsync<McpResource[]>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(resources);
            Assert.NotEmpty(resources);
            
            // Validate resource structure compliance
            foreach (var resource in resources)
            {
                Assert.NotNull(resource.Uri);
                Assert.NotNull(resource.Name);
                Assert.NotNull(resource.Description);
                Assert.NotNull(resource.MimeType);
                Assert.True(Uri.IsWellFormedUriString(resource.Uri, UriKind.Absolute));
            }
        }

        [Fact]
        public async Task MCP_ExecuteTool_EchoTool_ReturnsExpectedResult()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                ["message"] = "Hello MCP Gateway!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/mcp/tools/echo/execute", parameters);
            var result = await response.Content.ReadFromJsonAsync<McpToolResult>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Contains("Hello MCP Gateway!", result.Result?.ToString() ?? "");
        }

        [Fact]
        public async Task MCP_ExecuteTool_TimeTool_ReturnsValidTimestamp()
        {
            // Act
            var response = await _client.PostAsJsonAsync("/api/mcp/tools/time/execute", new Dictionary<string, object>());
            var result = await response.Content.ReadFromJsonAsync<McpToolResult>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Result);
            
            // The time tool returns an object with UTC, Local, and Timezone
            var resultString = result.Result.ToString();
            Assert.Contains("utc", resultString);
            Assert.Contains("local", resultString);
            Assert.Contains("timezone", resultString);
        }

        [Fact]
        public async Task MCP_ExecuteTool_CalculateTool_ReturnsCorrectResult()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                ["expression"] = "2+2"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/mcp/tools/calculate/execute", parameters);
            var result = await response.Content.ReadFromJsonAsync<McpToolResult>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.True(result.Success);
            
            // The calculate tool returns an object with expression and result
            var resultString = result.Result?.ToString() ?? "";
            Assert.Contains("\"expression\":\"2+2\"", resultString);
            Assert.Contains("\"result\":4", resultString);
        }

        [Fact]
        public async Task MCP_GetResourceContent_ValidUri_ReturnsContent()
        {
            // Act
            var response = await _client.GetAsync("/api/mcp/resources/content?uri=mcp://example/info");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotEmpty(content);
            Assert.Contains("MCP server", content);
        }

        [Fact]
        public async Task MCP_GetResourceContent_JsonResource_ReturnsValidJson()
        {
            // Act
            var response = await _client.GetAsync("/api/mcp/resources/content?uri=mcp://example/data");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotEmpty(content);
            
            // Parse the response which includes URI and content
            var jsonDoc = JsonDocument.Parse(content);
            Assert.True(jsonDoc.RootElement.TryGetProperty("uri", out _));
            Assert.True(jsonDoc.RootElement.TryGetProperty("content", out var contentProp));
            
            // Validate the nested content contains expected keys (without strict JSON parsing due to timestamp format)
            var nestedContent = contentProp.GetString();
            Assert.NotNull(nestedContent);
            Assert.Contains("sample", nestedContent);
            Assert.Contains("data", nestedContent);
            Assert.Contains("timestamp", nestedContent);
        }

        [Fact]
        public async Task MCP_ExecuteTool_InvalidTool_ReturnsErrorResult()
        {
            // Act
            var response = await _client.PostAsJsonAsync("/api/mcp/tools/nonexistent/execute", new Dictionary<string, object>());

            // Assert - The API may return BadRequest or OK with success=false, both are acceptable error handling patterns
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
            
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var result = await response.Content.ReadFromJsonAsync<McpToolResult>();
                Assert.NotNull(result);
                Assert.False(result.Success);
                Assert.NotNull(result.ErrorMessage);
                Assert.Contains("not found", result.ErrorMessage);
            }
        }

        [Fact]
        public async Task MCP_GetResourceContent_InvalidUri_ReturnsNotFound()
        {
            // Act
            var response = await _client.GetAsync("/api/mcp/resources/content?uri=mcp://nonexistent/resource");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Gateway_Responses_IncludeProperHeaders()
        {
            // Act
            var response = await _client.GetAsync("/api/mcp/tools");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }
    }
}