# Microsoft.Azure.Functions.Worker.Extensions.Mcp

This package provides triggers and bindings to support exposing Azure Functions .NET isolated functions as a Model Context Protocol (MCP) server.

## 🚀 Automatic Setup

**MCP is now enabled automatically!** Simply reference this package and MCP functionality will be available with StreamableHttp transport enabled by default.

## Quick Start

Basic usage - no explicit configuration needed:

``` csharp
var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();

// No explicit EnableMcp() call needed - MCP is enabled automatically!

builder.Build().Run();
```

For custom configuration, you can still explicitly configure MCP:

``` csharp
var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();

// Optional: Explicit configuration with custom settings
builder.EnableMcp(options => {
    options.EnableStreamableHttp = true; // Default: true
    options.EncryptClientState = false; // Default: false
});

builder.Build().Run();
```

## Example usage

The following is an example of how to use the MCP tool trigger in an Azure Functions application:
``` csharp
[Function(nameof(GetSnippet))]
public object GetSnippet(
    [McpToolTrigger(GetSnippetToolName, GetSnippetToolDescription)] ToolInvocationContext context,
    [BlobInput(BlobPath)] string snippetContent)
{
    return snippetContent;
}
```

You can also bind to the MCP tool property arguments using the `McpToolProperty` input binding as follows:
``` csharp
[Function(nameof(SaveSnippet))]
[BlobOutput(BlobPath)]
public string SaveSnippet(
    [McpToolTrigger(SaveSnippetToolName, SaveSnippetToolDescription)] ToolInvocationContext context,
    [McpToolProperty(SnippetNamePropertyName, PropertyType, SnippetNamePropertyDescription)] string name,
    [McpToolProperty(SnippetPropertyName, PropertyType, SnippetPropertyDescription)] string snippet)
{
    return snippet;
}
```

The above example will automatically expose the `name` and `snippet` parameters as properties for the MCP tool.

Alternatively, you can define properties when creating your application as follows:
``` csharp
builder.EnableMcp(); // Enables StreamableHttp by default
builder.EnableMcpToolMetadata();

// Define properties for the MCP tool:
builder.ConfigureMcpTool("SaveSnippet")
    .WithProperty(SnippetNamePropertyName, PropertyType, SnippetNamePropertyDescription);
```

## Configuration Options

### Custom Configuration (optional)

Since MCP is enabled automatically, custom configuration is optional:

``` csharp
builder.EnableMcp(options => {
    options.EnableStreamableHttp = true; // Default: true (recommended)
    options.EncryptClientState = false; // Default: false
});
```

### Enable Tool Properties (required for [McpToolProperty] attributes)

If you're using `[McpToolProperty]` attributes on function parameters, you need to explicitly enable metadata support:

``` csharp
builder.EnableMcp(); // Enable MCP transport
builder.EnableMcpToolMetadata(); // Enable property attribute processing
```

### Disable StreamableHttp (not recommended as SSE is deprecated)

``` csharp
builder.EnableMcp()
      .DisableStreamableHttp(); // Forces use of SSE transport
```

### Legacy Methods (still supported)

For specific scenarios, you can still use the targeted methods:

``` csharp
// Explicitly enable StreamableHttp (same as EnableMcp())
builder.EnableMcpStreamableHttp();

// Enable only tool metadata without transport
builder.EnableMcpToolMetadata();
```

## Architecture Notes

- **Auto-registration**: MCP functionality is automatically enabled when you reference this package
- **StreamableHttp default**: Modern transport is enabled by default (SSE is deprecated)  
- **Tool Properties**: Require explicit `EnableMcpToolMetadata()` call to process `[McpToolProperty]` attributes
- **MCP infrastructure endpoints**: Will appear in Functions host startup list (cosmetic only)
- **No conflicts**: Duplicate service registration is prevented automatically

The above example demonstrates how the properties for the tool named `SaveSnippet` are defined.