import { Asset, McpTool, McpResource, McpToolResult, ResourceContent } from './types';

export class ApiService {
  private static readonly API_KEY = 'dev-api-key-123'; // In production, this should come from environment or secure storage

  private static async fetchWithErrorHandling<T>(url: string, options?: RequestInit): Promise<T> {
    try {
      // Add API key header for MCP endpoints
      const headers = new Headers(options?.headers);
      if (url.startsWith('/api/mcp/')) {
        headers.set('X-API-Key', this.API_KEY);
      }

      const response = await fetch(url, {
        ...options,
        headers
      });
      
      if (!response.ok) {
        if (response.status === 401) {
          throw new Error('Authentication failed - invalid API key');
        }
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      
      return await response.json();
    } catch (error) {
      console.error(`API call failed: ${url}`, error);
      throw error;
    }
  }

  static async getAssets(): Promise<Asset[]> {
    return this.fetchWithErrorHandling<Asset[]>('/api/assets');
  }

  static async getMcpTools(): Promise<McpTool[]> {
    return this.fetchWithErrorHandling<McpTool[]>('/api/mcp/tools');
  }

  static async getMcpResources(): Promise<McpResource[]> {
    return this.fetchWithErrorHandling<McpResource[]>('/api/mcp/resources');
  }

  static async getResourceContent(uri: string): Promise<ResourceContent> {
    const encodedUri = encodeURIComponent(uri);
    return this.fetchWithErrorHandling<ResourceContent>(`/api/mcp/resources/content?uri=${encodedUri}`);
  }

  static async executeTool(toolName: string, parameters?: Record<string, any>): Promise<McpToolResult> {
    const encodedToolName = encodeURIComponent(toolName);
    return this.fetchWithErrorHandling<McpToolResult>(`/api/mcp/tools/${encodedToolName}/execute`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(parameters || {}),
    });
  }
}