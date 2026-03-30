## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Extensions <version>

- Upgraded ModelContextProtocol SDK dependency from `0.4.0-preview.3` to `1.2.0`
- Fixed typo in JSON property name `sessionId` on `Transport` class (#206)
- Add support for resource templates (#200)

### Microsoft.Azure.Functions.Worker.Extensions.Mcp <version>

#### Breaking Changes

- `McpFunctionMetadataTransformer` is now an internal class (#195)

#### Changes

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

#### Breaking Changes

- Upgraded ModelContextProtocol SDK dependency from `0.4.0-preview.3` to `1.2.0`. This surfaces the following upstream breaking changes to user code:
  - `ImageContentBlock.Data` and `AudioContentBlock.Data` changed from `string` to `ReadOnlyMemory<byte>`. Use `Encoding.UTF8.GetBytes(base64String)` when setting Data.
  - `BlobResourceContents.Blob` changed from `string` to `ReadOnlyMemory<byte>`. Use `BlobResourceContents.FromBytes(rawBytes, uri, mimeType)` factory method for construction.
  - `CallToolResult.StructuredContent` changed from `JsonNode?` to `JsonElement?`. Use `JsonSerializer.Deserialize<JsonElement>(json)` instead of `JsonNode.Parse(json)`.
