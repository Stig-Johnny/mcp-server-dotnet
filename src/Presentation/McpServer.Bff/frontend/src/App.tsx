import React, { useState, useEffect } from 'react';
import './App.css';

interface Asset {
  id: number;
  name: string;
  type: string;
  createdAt: string;
}

function App() {
  const [assets, setAssets] = useState<Asset[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchAssets = async () => {
      try {
        const response = await fetch('/api/assets');
        if (!response.ok) {
          throw new Error('Failed to fetch assets');
        }
        const data = await response.json();
        setAssets(data);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'An error occurred');
      } finally {
        setLoading(false);
      }
    };

    fetchAssets();
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
        <h2>Assets</h2>
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
      </main>
    </div>
  );
}

export default App;
