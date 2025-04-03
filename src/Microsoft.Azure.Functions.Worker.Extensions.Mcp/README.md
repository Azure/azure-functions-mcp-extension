# Microsoft.Azure.Functions.Worker.Extensions.Mcp

This package provides triggers and bindings to support exposing Azure Functions .NET isolated functions as a Model Context Protocol (MCP) server.

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
builder.EnableMcpToolMetadata();

// Define properties for the MCP tool:
builder.ConfigureMcpTool("SaveSnippet")
    .WithProperty(SnippetNamePropertyName, PropertyType, SnippetNamePropertyDescription);
```

The above example demonstrates how the properties for the tool named `SaveSnippet` are defined.