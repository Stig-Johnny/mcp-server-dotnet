export interface Asset {
  id: number;
  name: string;
  type: string;
  createdAt: string;
}

export interface McpTool {
  name: string;
  description: string;
  parameters: Record<string, any>;
  createdAt: string;
}

export interface McpResource {
  uri: string;
  name: string;
  description: string;
  mimeType: string;
  metadata: Record<string, any>;
}

export interface McpToolResult {
  toolName: string;
  success: boolean;
  result?: any;
  errorMessage?: string;
  executedAt: string;
}

export interface ResourceContent {
  uri: string;
  content: string;
}