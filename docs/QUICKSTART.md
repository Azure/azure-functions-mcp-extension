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
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- An MCP-compatible host (Claude Desktop, VS Code, ChatGPT, etc.)

## Quick Start: Your First MCP Tool

Let's start with a simple tool that returns the current time.

### 1. Create the Project

```bash
func init McpTimeApp --worker-runtime dotnet-isolated --target-framework net10.0
cd McpTimeApp
```

### 2. Add the MCP Extension

```bash
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Mcp
```

### 3. Create a Simple Tool

Create `GetTime.cs`:

```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

namespace McpTimeApp;

public class GetTime
{
    [Function(nameof(GetServerTime))]
    public string GetServerTime(
        [McpToolTrigger(nameof(GetServerTime), "Returns the current server time")]
        ToolInvocationContext context)
    {
        return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
    }
}
```

### 4. Run Locally

```bash
func start
```

The MCP server is now running at `http://localhost:7071/runtime/webhooks/mcp`.

### Test with VS Code

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
1. Open Copilot Chat and ask: "What time is it?" - Copilot will use your `GetServerTime` tool

## Building an MCP App with UI

Now let's build a complete MCP App with an interactive UI that displays the time.

### 1. Create the Tool with UI Metadata

The tool needs `_meta.ui.resourceUri` to link to the UI resource. Use `McpMetadata` attribute with a JSON string to add this:

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

### 2. Create the UI Resource

MCP Apps must be bundled into a single HTML file. We'll use Vite with `vite-plugin-singlefile` to bundle everything.

#### Set up the UI project

Create an `app` folder and initialize the project:

```bash
mkdir app && cd app
npm init -y
npm install @modelcontextprotocol/ext-apps
npm install -D typescript vite vite-plugin-singlefile
```

Create `app/vite.config.ts`:

```typescript
import { defineConfig } from 'vite';
import { viteSingleFile } from 'vite-plugin-singlefile';

export default defineConfig({
  plugins: [viteSingleFile()],
  build: {
    outDir: 'dist',
    emptyOutDir: true,
  },
});
```

Create `app/index.html`:

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
    <script type="module" src="/src/main.ts"></script>
</body>
</html>
```

Create `app/src/main.ts`:

```typescript
import { App } from '@modelcontextprotocol/ext-apps';

const timeEl = document.getElementById('time')!;
const refreshBtn = document.getElementById('refresh')!;

const app = new App({ name: 'Time Widget', version: '1.0.0' });

// Display tool result when received
app.ontoolresult = (result) => {
    const textContent = result.content?.find((c) => c.type === 'text');
    timeEl.textContent = (textContent as { text: string } | undefined)?.text || 'Unknown';
};

// Refresh button calls the tool again
refreshBtn.addEventListener('click', async () => {
    timeEl.textContent = 'Loading...';
    const result = await app.callServerTool({
        name: 'GetServerTime',
        arguments: {}
    });
    const textContent = result.content?.find((c) => c.type === 'text');
    timeEl.textContent = (textContent as { text: string } | undefined)?.text || 'Unknown';
});

app.connect();
```

#### Build the UI

```bash
cd app && npm run build && cd ..
```

This outputs a single bundled `app/dist/index.html` file.

#### Add the resource function

Update your `GetTime.cs` to serve the bundled HTML:

```csharp
using System;
using System.IO;
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

Make sure the bundled HTML file is copied to output by adding to your `.csproj`:

```xml
<ItemGroup>
    <None Update="app/dist/*">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
</ItemGroup>
```

### 3. Run and Test

```bash
func start
```

With your `.vscode/mcp.json` configured (see above), open Copilot Chat and ask "What time is it?". When the host calls `GetServerTime`, it:

1. Reads `_meta.ui.resourceUri` to find `ui://time/index.html`
2. Fetches the resource and renders it in an iframe
3. Passes the tool result to the UI via `ontoolresult`

## Key Concepts

### McpToolTrigger

Defines an MCP tool that AI models can call:

```csharp
[McpToolTrigger("tool-name", "Description for the AI model")]
```

### McpToolProperty

Binds function parameters to tool input:

```csharp
[Function(nameof(GetWeather))]
public async Task<string> GetWeather(
    [McpToolTrigger(nameof(GetWeather), "Get weather for a location")] ToolInvocationContext context,
    [McpToolProperty("location", "City name (e.g., Seattle, London)")] string location)
{
    // location is automatically populated from tool arguments
    return await FetchWeather(location);
}
```

### McpResourceTrigger

Defines an MCP resource that can be read by clients:

```csharp
[McpResourceTrigger(
    "ui://my-app/widget.html",  // Resource URI
    "Widget Name",              // Display name
    MimeType = "text/html;profile=mcp-app",  // Required for MCP Apps
    Description = "Description of the resource")]
```

The `ui://` scheme and `text/html;profile=mcp-app` MIME type tell hosts this is an MCP App resource.

### McpMetadata

Adds metadata to tools or resources using a JSON string. This allows you to specify complex nested metadata structures:

```csharp
// For tools - link to UI resource
private const string ToolMetadata = """
    {
        "ui": {
            "resourceUri": "ui://my-app/widget.html"
        }
    }
    """;

[McpToolTrigger("my-tool", "Description")]
[McpMetadata(ToolMetadata)]
```

```csharp
// For resources - add custom metadata
private const string ResourceMetadata = """
    {
        "author": "Your Name",
        "version": "1.0.0",
        "tags": ["example", "demo"]
    }
    """;

[McpResourceTrigger("uri://resource", "Name")]
[McpMetadata(ResourceMetadata)]
```

## Building the UI

MCP Apps must be bundled into a single HTML file. The UI uses the [@modelcontextprotocol/ext-apps](https://www.npmjs.com/package/@modelcontextprotocol/ext-apps) package for communication with the MCP host:

```typescript
import { App } from "@modelcontextprotocol/ext-apps";

const app = new App({ name: "My App", version: "1.0.0" });

// Receive tool results from the host
app.ontoolresult = (result) => {
    const data = result.content?.find(c => c.type === "text")?.text;
    // Update your UI with the data
};

// Call server tools from the UI
const response = await app.callServerTool({
    name: "my-tool",
    arguments: { param: "value" }
});

// Update model context (the AI sees this)
await app.updateModelContext({
    content: [{ type: "text", text: "User clicked option A" }]
});

await app.connect();
```

### Bundling with Vite

Use Vite with `vite-plugin-singlefile` to bundle your UI into a single HTML file:

```bash
npm install @modelcontextprotocol/ext-apps
npm install -D typescript vite vite-plugin-singlefile
```

Create a `vite.config.ts`:

```typescript
import { defineConfig } from 'vite';
import { viteSingleFile } from 'vite-plugin-singlefile';

export default defineConfig({
  plugins: [viteSingleFile()],
  build: {
    outDir: 'dist',
    emptyOutDir: true,
  },
});
```

Then build with `npm run build` (add `"build": "vite build"` to your package.json scripts).

See the [McpTimeApp example](../test/McpTimeApp/) for a complete setup.

## Text Resources

Not all resources need to be MCP Apps. You can also serve plain text, JSON, or other content:

```csharp
[Function(nameof(GetConfig))]
public string GetConfig(
    [McpResourceTrigger(
        "config://app/settings",
        "App Settings",
        MimeType = "application/json")]
    ResourceInvocationContext context)
{
    return JsonSerializer.Serialize(new { theme = "dark", language = "en" });
}
```

## Deploying to Azure

Deploy your MCP server to Azure Functions:

```bash
func azure functionapp publish <your-function-app-name>
```

Then configure your MCP client to connect to your deployed endpoint.

## Securing with Authentication (EasyAuth)

Azure Functions provides built-in authentication that you can enable without writing any code. This is useful when your MCP App is deployed to Azure and you want to require users to authenticate before accessing it.

> **Note:** Some MCP hosts like ChatGPT require [Dynamic Client Registration (DCR)](https://openid.net/specs/openid-connect-registration-1_0.html). Auth0 is a good choice as it provides DCR out-of-the-box and is a certified OpenID Connect provider.

### 1. Set Up Auth0

1. Create an [Auth0](https://auth0.com/) account
2. Create a **Regular Web Application** (Applications → Applications → Create Application)
3. In the app's **Settings** tab:
   - Add your callback URL to **Allowed Callback URLs** (e.g., `https://chatgpt.com/connector_platform_oauth_redirect` for ChatGPT)
   - Note the **Client ID** and **Client Secret**
4. Create an **API** (Applications → APIs → Create API):
   - Set **Identifier** to your app's Client ID
   - Enable **Allow Offline Access** in the API Settings
5. In **Settings → General**, set **Default Audience** to your API
6. In **Settings → Advanced**, enable **Dynamic Client Registration**
7. In **Authentication → Database**, enable **Promote Connection to Domain Level**

### 2. Enable Azure Functions Built-in Auth

1. In the Azure Portal, open your Function App
2. Go to **Settings → Authentication → Add identity provider**
3. Configure the provider:
   - **Identity provider:** OpenID Connect
   - **OpenID provider name:** A friendly name (e.g., `auth0`)
   - **Metadata URL:** `https://<your-auth0-domain>/.well-known/openid-configuration`
   - **Client ID:** Your Auth0 app's Client ID
   - **Client secret:** Your Auth0 app's Client Secret
   - **Restrict access:** Require authentication
   - **Unauthenticated requests:** HTTP 401 Unauthorized
4. Click **Add**

### 3. Configure Required Scopes

1. In your Function App, go to **Settings → Environment variables**
2. Add a new setting:
   - **Name:** `WEBSITE_AUTH_PRM_DEFAULT_WITH_SCOPES`
   - **Value:** `openid,profile,email`
3. Click **Apply** and confirm

Your MCP server now requires authentication. When connecting from an MCP host that supports OAuth (like ChatGPT), users will be prompted to sign in.

## Next Steps

- Read the [MCP Apps specification](https://modelcontextprotocol.io/docs/extensions/apps)
- Explore the [@modelcontextprotocol/ext-apps API](https://modelcontextprotocol.github.io/ext-apps/api/)
- Check out [more MCP App examples](https://github.com/modelcontextprotocol/ext-apps/tree/main/examples)

## Supported MCP Hosts

MCP Apps work in:

- Claude (web and desktop)
- Visual Studio Code (Insiders)
- ChatGPT
- Goose
- Any host implementing the [MCP Apps extension](https://modelcontextprotocol.io/docs/extensions/apps)
