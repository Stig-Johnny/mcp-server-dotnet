using McpServer.Bff.Configuration;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;

namespace McpServer.Bff.Services;

public interface IMcpApiService
{
    Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default);
    Task<T?> PostAsync<T>(string endpoint, object? data = null, CancellationToken cancellationToken = default);
}

public class McpApiService : IMcpApiService
{
    private readonly HttpClient _httpClient;
    private readonly McpApiConfiguration _config;
    private readonly ILogger<McpApiService> _logger;

    public McpApiService(HttpClient httpClient, IOptions<McpApiConfiguration> config, ILogger<McpApiService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Configure HTTP client with base URL and headers
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _config.ApiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Making GET request to MCP API: {Endpoint}", endpoint);
            
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("MCP API GET request failed: {StatusCode} - {Endpoint}", 
                    response.StatusCode, endpoint);
                return default;
            }

            var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<T>(jsonString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making GET request to MCP API: {Endpoint}", endpoint);
            return default;
        }
    }

    public async Task<T?> PostAsync<T>(string endpoint, object? data = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Making POST request to MCP API: {Endpoint}", endpoint);
            
            var json = data != null ? JsonSerializer.Serialize(data) : "{}";
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("MCP API POST request failed: {StatusCode} - {Endpoint}", 
                    response.StatusCode, endpoint);
                return default;
            }

            var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<T>(jsonString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making POST request to MCP API: {Endpoint}", endpoint);
            return default;
        }
    }
}