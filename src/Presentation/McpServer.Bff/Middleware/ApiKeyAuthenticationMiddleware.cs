namespace McpServer.Bff.Middleware;

public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;

    public ApiKeyAuthenticationMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip authentication for certain paths
        if (ShouldSkipAuthentication(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Check for API key in headers
        if (!context.Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
        {
            _logger.LogWarning("API key missing for request: {Path}", context.Request.Path);
            await WriteUnauthorizedResponse(context, "API key is required");
            return;
        }

        var providedApiKey = apiKeyHeader.FirstOrDefault();
        var expectedApiKey = _configuration["McpApi:ApiKey"];

        if (string.IsNullOrEmpty(providedApiKey) || providedApiKey != expectedApiKey)
        {
            _logger.LogWarning("Invalid API key for request: {Path}", context.Request.Path);
            await WriteUnauthorizedResponse(context, "Invalid API key");
            return;
        }

        await _next(context);
    }

    private static bool ShouldSkipAuthentication(PathString path)
    {
        // Skip authentication for these paths
        var skipPaths = new[]
        {
            "/health",
            "/health/ready",
            "/swagger",
            "/api/assets", // Keep assets endpoint open for demo purposes
            "/static/", // Static files
            "/manifest.json",
            "/favicon.ico"
        };

        // Skip authentication for static files and React app routing
        if (skipPaths.Any(skipPath => path.StartsWithSegments(skipPath, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // Skip authentication for React app routing (any path that doesn't start with /api/mcp)
        if (!path.StartsWithSegments("/api/mcp", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static async Task WriteUnauthorizedResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";
        
        var response = new { error = "Unauthorized", message };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
}