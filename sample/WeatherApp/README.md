# Weather App Sample

A sample MCP App that displays weather information with an interactive UI.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (for building the UI)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)

## Getting Started

### 1. Build the UI

The UI must be bundled before running the function app:

```bash
cd app
npm install
npm run build
cd ..
```

This creates a bundled `app/dist/index.html` file that the function serves.

### 2. Run the Function App

```bash
func start
```

The MCP server will be available at `http://localhost:7071/runtime/webhooks/mcp`.

### 3. Connect from VS Code

Create a `.vscode/mcp.json` file:

```json
{
    "servers": {
        "weather-app": {
            "type": "http",
            "url": "http://localhost:7071/runtime/webhooks/mcp"
        }
    }
}
```

Then use **MCP: List Servers** from the Command Palette to connect, and ask Copilot: "What's the weather in Seattle?"