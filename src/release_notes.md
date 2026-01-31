## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Extensions <version>

- <entry>

### Microsoft.Azure.Functions.Worker.Extensions.Mcp 1.2.0-preview.2

- Added fluent configuration APIs (`ConfigureMcpResource` and `WithMeta`) to configure MCP tool and resource metadata at startup (#195)

    ```csharp
    // Configure MCP resource metadata using the fluent API
    builder
        .ConfigureMcpResource("hellopage")
        .WithMeta("ui", new { prefersBorder = true });


    // Configure MCP tool metadata using the fluent API
    builder
        .ConfigureMcpTool("sayhello")
        .WithProperty("name", McpToolPropertyType.String, "Name of the user", required: true)
        .WithMeta("ui", new { resourceUri = "ui://index.html" });
    ```

### Microsoft.Azure.Functions.Worker.Extensions.Mcp.Sdk 1.0.0-preview.3

- Updated `Microsoft.Azure.Functions.Worker.Extensions.Mcp.Sdk` to take a dependency on `Microsoft.Azure.Functions.Worker.Extensions.Mcp` (#181)
