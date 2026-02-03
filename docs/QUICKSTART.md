# Getting Started with MCP Apps on Azure Functions

Build interactive MCP Apps using Azure Functions. This guide walks you through creating MCP tools and resources that render rich UI directly in AI assistants like Claude, ChatGPT, VS Code, and more.

## What Are MCP Apps?

[MCP Apps](https://blog.modelcontextprotocol.io/posts/2026-01-26-mcp-apps/) let tools return interactive interfaces instead of plain text. When a tool declares a UI resource, the host renders it in a sandboxed iframe where users can interact directly.

### MCP Apps = Tool + UI Resource

The architecture relies on two MCP primitives:

1. **Tools** with UI metadata pointing to a resource URI
2. **Resources** containing bundled HTML/JavaScript served via the `ui://` scheme

Azure Functions makes it easy to build both.

## Prerequisites

This guide will walk you through an example using .NET.

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Node.js 18+.
- Azure Storage Emulator (azurite)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- An MCP-compatible host (Claude Desktop, VS Code Insider, ChatGPT)

## Quick Start: Build an MCP App

Let's build an MCP App with an interactive UI that displays the current server time.

### 1. Create the Project

```bash
func init McpTimeApp --worker-runtime dotnet-isolated --target-framework net10.0
cd McpTimeApp
```

### 2. Add the MCP Extension

```bash
# dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Mcp

# For now you have to specify preview version
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Mcp --version 1.2.0-preview.1
```

### 3. Create the UI

MCP Apps must be bundled into a single HTML file. We'll use Vite with `vite-plugin-singlefile` to bundle everything.

#### Set up the UI project

Create an `app` folder inside `McpTimeApp` and initialize the project:

```bash
mkdir app && cd app
npm init -y
npm install @modelcontextprotocol/ext-apps
npm install -D typescript vite vite-plugin-singlefile cross-env
```

Configure package.json

```bash
npm pkg set type=module
npm pkg set scripts.build="tsc --noEmit && cross-env INPUT=index.html vite build"
```

Create `app/tsconfig.json`:

```json
{
  "compilerOptions": {
    "target": "ES2022",
    "module": "ESNext",
    "moduleResolution": "bundler",
    "strict": true,
    "esModuleInterop": true,
    "skipLibCheck": true,
    "forceConsistentCasingInFileNames": true,
    "lib": ["ES2022", "DOM", "DOM.Iterable"],
    "noEmit": true
  },
  "include": ["src/**/*", "vite.config.ts"]
}
```

Create `app/vite.config.ts`:

```typescript
import { defineConfig } from 'vite';
import { viteSingleFile } from 'vite-plugin-singlefile';

export default defineConfig({
  root: 'src',
  plugins: [viteSingleFile()],
  build: {
    outDir: '../dist',
    emptyOutDir: true,
  },
});
```

Create `app/src/index.html`:

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Time Widget</title>
    <style>
        body {
            font-family: system-ui, sans-serif;
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            margin: 0;
            background: linear-gradient(135deg, #1e3a5f, #0f172a);
            color: white;
        }
        .widget {
            text-align: center;
            padding: 2rem;
            background: rgba(255,255,255,0.1);
            border-radius: 16px;
            backdrop-filter: blur(10px);
        }
        .time { font-size: 2.5rem; font-weight: bold; margin: 1rem 0; }
        button {
            padding: 0.75rem 1.5rem;
            font-size: 1rem;
            border: none;
            border-radius: 8px;
            background: #3b82f6;
            color: white;
            cursor: pointer;
        }
        button:hover { background: #2563eb; }
    </style>
</head>
<body>
    <div class="widget">
        <h2>🕐 Server Time</h2>
        <div class="time" id="time">Loading...</div>
        <button id="refresh">Refresh</button>
    </div>
    <script type="module" src="./main.ts"></script>
</body>
</html>
```

Create `app/src/main.ts`:

```typescript
import { App } from '@modelcontextprotocol/ext-apps';

// Get element references
const timeEl = document.getElementById('time')!;
const refreshBtn = document.getElementById('refresh')!;

const app = new App({ name: 'Time Widget', version: '1.0.0' });

// Handle tool results from the server. Set before `app.connect()` to avoid
// missing the initial tool result.
app.ontoolresult = (result) => {
    const textContent = result.content?.find((c) => c.type === 'text');
    timeEl.textContent = (textContent as { text: string } | undefined)?.text || 'Unknown';
};

// Refresh button calls the tool again
refreshBtn.addEventListener('click', async () => {
    timeEl.textContent = 'Loading...';
    // `app.callServerTool()` lets the UI request fresh data from the server
    const result = await app.callServerTool({
        name: 'GetServerTime',
        arguments: {}
    });
    const textContent = result.content?.find((c) => c.type === 'text');
    timeEl.textContent = (textContent as { text: string } | undefined)?.text || 'Unknown';
});

// Connect to host
app.connect();
```

#### Build the UI

```bash
npm run build
```

This outputs a single bundled `app/dist/index.html` file.

### 4. Add UI to Project

Make sure the bundled HTML file is copied to output by adding to your `.csproj`:

```xml
<ItemGroup>
    <None Update="app/dist/*">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
</ItemGroup>
```

### 5. Create the Tool and Resource Functions

Back in `McpTimeApp` root, create `TimeApp.cs` with both the tool and resource functions:

```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

namespace McpTimeApp;

public class TimeApp
{
    private const string ToolMetadata = """
        {
            "ui": {
                "resourceUri": "ui://time/index.html"
            }
        }
        """;

    [Function(nameof(GetTimeWidget))]
    public string GetTimeWidget(
        [McpResourceTrigger(
            "ui://time/index.html",
            "Time Widget",
            MimeType = "text/html;profile=mcp-app",
            Description = "Interactive time display for MCP Apps")]
        ResourceInvocationContext context)
    {
        var file = Path.Combine(AppContext.BaseDirectory, "app", "dist", "index.html");
        return File.ReadAllText(file);
    }

    [Function(nameof(GetServerTime))]
    public string GetServerTime(
        [McpToolTrigger(nameof(GetServerTime), "Returns the current server time")]
        [McpMetadata(ToolMetadata)]
        ToolInvocationContext context)
    {
        return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
    }
}
```

The tool uses `McpMetadata` to declare a UI resource via `_meta.ui.resourceUri`. The resource function serves the bundled HTML with the MCP Apps MIME type.

### 6. Run and Test

First make sure to start the storage emulator:

```bash
azurite
```

Run the function app:

```bash
func start
```

The MCP server is now running at `http://localhost:7071/runtime/webhooks/mcp`.

#### Test with VS Code Insider

> Note: MCP Apps/UI is currently only supported in VS Code Insider. You can alternatively use another MCP client to test your app.

1. Create a `.vscode/mcp.json` file in your project:

```json
{
    "servers": {
        "local-mcp-function": {
            "type": "http",
            "url": "http://localhost:7071/runtime/webhooks/mcp"
        }
    }
}
```

1. Open the Command Palette (`Cmd+Shift+P` / `Ctrl+Shift+P`) and run **MCP: List Servers**
1. Select `local-mcp-function` to connect
1. Open Copilot Chat and ask: "What time is it?"

## Next Steps

- TODO:// Add doc link for ms learn azure funcions tool and resource triggers
- TODO:// Add doc link for deploying to azure
- TODO:// Add doc link for auth
- Read the [MCP Apps specification](https://modelcontextprotocol.io/docs/extensions/apps)
- Explore the [@modelcontextprotocol/ext-apps API](https://modelcontextprotocol.github.io/ext-apps/api/)
