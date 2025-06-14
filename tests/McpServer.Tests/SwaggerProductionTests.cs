using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace McpServer.Tests
{
    public class SwaggerProductionTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public SwaggerProductionTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task SwaggerJson_ReturnsOkInProduction()
        {
            // Arrange
            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
            }).CreateClient();

            // Act
            var response = await client.GetAsync("/swagger/v1/swagger.json");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("MCP Server API", content);
            Assert.Contains("\"openapi\":", content);
        }

        [Fact]
        public async Task SwaggerUI_ReturnsOkInProduction()
        {
            // Arrange
            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
            }).CreateClient();

            // Act
            var response = await client.GetAsync("/swagger");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("swagger-ui", content);
        }

        [Fact]
        public async Task SwaggerJson_ReturnsOkInStaging()
        {
            // Arrange
            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Staging");
            }).CreateClient();

            // Act
            var response = await client.GetAsync("/swagger/v1/swagger.json");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("MCP Server API", content);
        }
    }
}