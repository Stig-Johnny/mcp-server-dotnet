import React, { useState, useEffect } from 'react';
import './App.css';

interface Asset {
  id: number;
  name: string;
  type: string;
  createdAt: string;
}

interface McpTool {
  name: string;
  description: string;
  parameters: Record<string, any>;
  createdAt: string;
}

interface McpResource {
  uri: string;
  name: string;
  description: string;
  mimeType: string;
  metadata: Record<string, any>;
}

function App() {
  const [assets, setAssets] = useState<Asset[]>([]);
  const [tools, setTools] = useState<McpTool[]>([]);
  const [resources, setResources] = useState<McpResource[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
        // Fetch assets
        const assetsResponse = await fetch('/api/assets');
        if (!assetsResponse.ok) {
          throw new Error('Failed to fetch assets');
        }
        const assetsData = await assetsResponse.json();
        setAssets(assetsData);

        // Fetch MCP tools
        const toolsResponse = await fetch('/api/mcp/tools');
        if (!toolsResponse.ok) {
          throw new Error('Failed to fetch MCP tools');
        }
        const toolsData = await toolsResponse.json();
        setTools(toolsData);

        // Fetch MCP resources
        const resourcesResponse = await fetch('/api/mcp/resources');
        if (!resourcesResponse.ok) {
          throw new Error('Failed to fetch MCP resources');
        }
        const resourcesData = await resourcesResponse.json();
        setResources(resourcesData);

      } catch (err) {
        setError(err instanceof Error ? err.message : 'An error occurred');
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, []);

  if (loading) {
    return (
      <div className="App">
        <header className="App-header">
          <h1>MCP Server BFF</h1>
          <p>Loading data...</p>
        </header>
      </div>
    );
  }

  if (error) {
    return (
      <div className="App">
        <header className="App-header">
          <h1>MCP Server BFF</h1>
          <p style={{ color: 'red' }}>Error: {error}</p>
        </header>
      </div>
    );
  }

  return (
    <div className="App">
      <header className="App-header">
        <h1>MCP Server BFF</h1>
        <p>Backend-for-Frontend with React</p>
      </header>
      <main style={{ padding: '20px', color: '#333' }}>
        <h2>Assets</h2>
        <div style={{ display: 'grid', gap: '15px', gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))', marginBottom: '40px' }}>
          {assets.map((asset) => (
            <div
              key={asset.id}
              style={{
                border: '1px solid #ddd',
                borderRadius: '8px',
                padding: '15px',
                background: '#f9f9f9'
              }}
            >
              <h3>{asset.name}</h3>
              <p><strong>Type:</strong> {asset.type}</p>
              <p><strong>Created:</strong> {new Date(asset.createdAt).toLocaleDateString()}</p>
            </div>
          ))}
        </div>

        <h2>MCP Tools</h2>
        <div style={{ display: 'grid', gap: '15px', gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))', marginBottom: '40px' }}>
          {tools.map((tool) => (
            <div
              key={tool.name}
              style={{
                border: '1px solid #007acc',
                borderRadius: '8px',
                padding: '15px',
                background: '#f0f8ff'
              }}
            >
              <h3>{tool.name}</h3>
              <p><strong>Description:</strong> {tool.description}</p>
              <p><strong>Parameters:</strong> {Object.keys(tool.parameters).length > 0 ? Object.keys(tool.parameters).join(', ') : 'None'}</p>
              <p><strong>Created:</strong> {new Date(tool.createdAt).toLocaleDateString()}</p>
            </div>
          ))}
        </div>

        <h2>MCP Resources</h2>
        <div style={{ display: 'grid', gap: '15px', gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))' }}>
          {resources.map((resource) => (
            <div
              key={resource.uri}
              style={{
                border: '1px solid #28a745',
                borderRadius: '8px',
                padding: '15px',
                background: '#f8fff8'
              }}
            >
              <h3>{resource.name}</h3>
              <p><strong>Description:</strong> {resource.description}</p>
              <p><strong>URI:</strong> {resource.uri}</p>
              <p><strong>MIME Type:</strong> {resource.mimeType}</p>
              {Object.keys(resource.metadata).length > 0 && (
                <p><strong>Metadata:</strong> {Object.keys(resource.metadata).join(', ')}</p>
              )}
            </div>
          ))}
        </div>
      </main>
    </div>
  );
}

export default App;
