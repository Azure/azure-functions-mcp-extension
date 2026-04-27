## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Extensions 1.5.0

#### Breaking Changes

- `McpInputSchemaJsonUtilities` is now `internal` (was `public`). It was never intended as a public API. (#245)

#### Changes

- Validate required prompt arguments (#244)
- Added output schema support on the tool trigger (#245)
- Added `UseResultSchema` to `McpPromptTriggerAttribute`. When set by the worker, the host unwraps the `McpPromptResult` envelope produced by the worker instead of inferring the shape from the JSON. (#212)

### Microsoft.Azure.Functions.Worker.Extensions.Mcp 1.5.0-preview.1

#### Bug Fixes

- Fixed `DateTime` tool parameters failing to bind when the JSON input contains an ISO 8601 date string. The `DictionaryStringObjectJsonConverter` was deserializing date strings as `DateTimeOffset`, which had no conversion path to `DateTime`. Added explicit `DateTimeOffset` ↔ `DateTime` conversions in `McpInputConversionHelper`.
- Fixed MCP App `WithView(filePath)` failing on Azure with `DirectoryNotFoundException` by resolving relative paths against `AppContext.BaseDirectory` instead of the process working directory.

#### Changes

- Add `WithInputSchema` fluent API for MCP tools (#243)

    Lets users provide an explicit JSON input schema for an MCP tool via `McpToolBuilder.WithInputSchema(string|JsonNode)`.

    ```csharp
    builder.ConfigureMcpTool("MyTool")
        .WithInputSchema(new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["name"] = new JsonObject
                {
                    ["type"] = "string",
                    ["description"] = "The user's name"
                }
            },
            ["required"] = new JsonArray("name")
        });
    ```

- Implement fluent API for building MCP Apps (#226)

    This feature introduces first-class support for MCP App tools in the MCP extension, enabling tools to be configured as apps with UI views, static assets, and visibility controls. It adds a new configuration model for MCP Apps, emits synthetic resource functions for app views, and includes robust validation and security features for app configuration and static asset serving.

    ```csharp
    // 1. Define an MCP tool function
    [Function(nameof(HelloApp))]
        public string HelloApp(
            [McpToolTrigger("HelloApp", "A simple MCP App that says hello.")] ToolInvocationContext context)
        {
            _logger.LogInformation("HelloApp tool invoked.");
            return "Hello from app";
        }

    // 2. Configure the tool as an app in Program.cs
    builder.ConfigureMcpTool("HelloApp")
        .AsMcpApp(app => app
            .WithView("assets/hello-app.html")
            .WithTitle("Hello App")
            .WithPermissions(McpAppPermissions.ClipboardWrite | McpAppPermissions.ClipboardRead)
            .WithCsp(csp =>
            {
                csp.AllowBaseUri("https://www.microsoft.com")
                .ConnectTo("https://www.microsoft.com");
            }));
    ```

### Microsoft.Azure.Functions.Worker.Extensions.Mcp.Sdk <version>

- Upgraded MCP C# SDK dependency from 0.4.0-preview.3 to 1.2.0 (#222)
- Add support for strongly-typed prompt trigger return types: `GetPromptResult`, `PromptMessage`, and `IList<PromptMessage>` (#212)
