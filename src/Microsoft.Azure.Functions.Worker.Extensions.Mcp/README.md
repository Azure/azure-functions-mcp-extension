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

## Configuring Tool Input Schema

You can configure the input schema for an MCP tool in several ways using `ConfigureMcpTool()`. These approaches are **mutually exclusive** — you must choose one per tool.

### Option 1: Define individual properties with `WithProperty()`

Use `WithProperty()` to define tool properties one at a time. The input schema is automatically generated from the configured properties.

``` csharp
builder.ConfigureMcpTool("SaveSnippet")
    .WithProperty("name", McpToolPropertyType.String, "The name of the snippet", required: true)
    .WithProperty("snippet", McpToolPropertyType.String, "The snippet content", required: true);
```

### Option 2: Generate schema from a CLR type with `WithInputSchema<T>()`

Use `WithInputSchema<T>()` to generate the JSON input schema automatically from a C# class, record, or struct. The schema is derived from the type's public properties using `JsonSchemaExporter`.

``` csharp
public class SaveSnippetInput
{
    public required string Name { get; set; }
    public required string Snippet { get; set; }
    public string? Language { get; set; }
}

builder.ConfigureMcpTool("SaveSnippet")
    .WithInputSchema<SaveSnippetInput>();
```

You can also pass a `Type` directly:

``` csharp
builder.ConfigureMcpTool("SaveSnippet")
    .WithInputSchema(typeof(SaveSnippetInput));
```

Optionally, provide custom `JsonSerializerOptions` to control schema generation:

``` csharp
var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
builder.ConfigureMcpTool("SaveSnippet")
    .WithInputSchema<SaveSnippetInput>(options);
```

### Option 3: Provide an explicit JSON schema with `WithInputSchema(string)`

Use `WithInputSchema(string)` to pass a raw JSON schema string directly. The schema must have root `"type": "object"`.

``` csharp
builder.ConfigureMcpTool("SaveSnippet")
    .WithInputSchema("""
    {
        "type": "object",
        "properties": {
            "name": { "type": "string", "description": "The name of the snippet" },
            "snippet": { "type": "string", "description": "The snippet content" }
        },
        "required": ["name", "snippet"]
    }
    """);
```

### Option 4: Provide a `JsonNode` schema with `WithInputSchema(JsonNode)`

Use `WithInputSchema(JsonNode)` to pass a pre-built `JsonNode` directly. This is useful when you already have a schema as a `JsonNode` — for example, from programmatic construction or an external source.

``` csharp
var schemaNode = JsonNode.Parse("""
{
    "type": "object",
    "properties": {
        "name": { "type": "string", "description": "The name of the snippet" },
        "snippet": { "type": "string", "description": "The snippet content" }
    },
    "required": ["name", "snippet"]
}
""");

builder.ConfigureMcpTool("SaveSnippet")
    .WithInputSchema(schemaNode!);
```

### Automatic schema inference (default)

If none of the above methods are used, the input schema is inferred automatically from the function's method signature using `McpToolProperty` bindings:

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

> **Note:** `WithProperty()` and `WithInputSchema()` cannot be combined on the same tool. Attempting to use both will throw an `InvalidOperationException`.