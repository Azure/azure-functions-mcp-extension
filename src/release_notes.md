## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Extensions <version>

- Added structured content support for tools (#172)

### Microsoft.Azure.Functions.Worker.Extensions.Mcp <version>

- Added structured content support for tools (#172)
- Added fluent configuration APIs (`ConfigureMcpResource` and `WithMetadata`) to configure MCP tool and resource metadata at startup (#195)

    ```csharp
    // Configure MCP resource metadata using the fluent API
    builder
        .ConfigureMcpResource("ui://my/welcomepage.html")
        .WithMetadata("ui", new { prefersBorder = true });


    // Configure MCP tool metadata using the fluent API
    builder
        .ConfigureMcpTool("sayhello")
        .WithProperty("name", McpToolPropertyType.String, "Name of the user", required: true)
        .WithMetadata("ui", new { resourceUri = "ui://index.html" });
    ```

### Microsoft.Azure.Functions.Worker.Extensions.Mcp.Sdk <version>

- Added structured content support for tools (#172)
