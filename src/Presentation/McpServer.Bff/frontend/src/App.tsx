import React, { useState, useEffect } from 'react';
import './App.css';
import { Asset, McpTool, McpResource } from './types';
import { ApiService } from './apiService';

function App() {
  const [assets, setAssets] = useState<Asset[]>([]);
  const [mcpTools, setMcpTools] = useState<McpTool[]>([]);
  const [mcpResources, setMcpResources] = useState<McpResource[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        setError(null);
        
        // Fetch all data concurrently
        const [assetsData, toolsData, resourcesData] = await Promise.allSettled([
          ApiService.getAssets(),
          ApiService.getMcpTools(),
          ApiService.getMcpResources()
        ]);

        // Handle assets
        if (assetsData.status === 'fulfilled') {
          setAssets(assetsData.value);
        } else {
          console.error('Failed to fetch assets:', assetsData.reason);
        }

        // Handle MCP tools
        if (toolsData.status === 'fulfilled') {
          setMcpTools(toolsData.value);
        } else {
          console.error('Failed to fetch MCP tools:', toolsData.reason);
        }

        // Handle MCP resources
        if (resourcesData.status === 'fulfilled') {
          setMcpResources(resourcesData.value);
        } else {
          console.error('Failed to fetch MCP resources:', resourcesData.reason);
        }

        // Set error only if all requests failed
        if (assetsData.status === 'rejected' && toolsData.status === 'rejected' && resourcesData.status === 'rejected') {
          setError('Failed to fetch data from all APIs');
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : 'An unexpected error occurred');
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
          <p>Loading assets...</p>
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
        
        {/* Assets Section */}
        <section style={{ marginBottom: '40px' }}>
          <h2>Assets</h2>
          {assets.length > 0 ? (
            <div style={{ display: 'grid', gap: '15px', gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))' }}>
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
          ) : (
            <p>No assets available</p>
          )}
        </section>

        {/* MCP Tools Section */}
        <section style={{ marginBottom: '40px' }}>
          <h2>MCP Tools</h2>
          {mcpTools.length > 0 ? (
            <div style={{ display: 'grid', gap: '15px', gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))' }}>
              {mcpTools.map((tool) => (
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
                  <p>{tool.description}</p>
                  <p><strong>Parameters:</strong> {Object.keys(tool.parameters).length} parameter(s)</p>
                  <p><strong>Created:</strong> {new Date(tool.createdAt).toLocaleDateString()}</p>
                </div>
              ))}
            </div>
          ) : (
            <p>No MCP tools available</p>
          )}
        </section>

        {/* MCP Resources Section */}
        <section style={{ marginBottom: '40px' }}>
          <h2>MCP Resources</h2>
          {mcpResources.length > 0 ? (
            <div style={{ display: 'grid', gap: '15px', gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))' }}>
              {mcpResources.map((resource) => (
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
                  <p>{resource.description}</p>
                  <p><strong>URI:</strong> <code style={{ fontSize: '0.9em' }}>{resource.uri}</code></p>
                  <p><strong>MIME Type:</strong> {resource.mimeType}</p>
                </div>
              ))}
            </div>
          ) : (
            <p>No MCP resources available</p>
          )}
        </section>

      </main>
    </div>
  );
}

export default App;
