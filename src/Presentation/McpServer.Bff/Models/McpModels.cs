namespace McpServer.Bff.Models;

public class McpTool
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class McpResource
{
    public string Uri { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class McpToolResult
{
    public string ToolName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public object? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ExecutedAt { get; set; }
}

public class ResourceContent
{
    public string Uri { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}