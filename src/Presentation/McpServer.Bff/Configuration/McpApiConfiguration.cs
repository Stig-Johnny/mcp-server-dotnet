namespace McpServer.Bff.Configuration;

public class McpApiConfiguration
{
    public const string SectionName = "McpApi";
    
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}