# MCP Host Extension Specification

This document describes the binding contract and interfaces for the Azure Functions MCP Host Extension (`Microsoft.Azure.Functions.Extensions.Mcp`). Use this specification when developing extensions (such as worker extensions) that interface with the host extension.

## Overview

The MCP Host Extension provides triggers and bindings to expose Azure Functions applications as Model Context Protocol (MCP) servers. It handles:

- Tool registration and invocation
- Resource registration and reading
- Session management
- Transport abstraction (HTTP Streamable, SSE)
- Metadata propagation

## Binding Types

### Tool Trigger Binding

**Binding Type:** `mcpToolTrigger`

Used to define MCP tools that can be invoked by MCP clients.

#### Trigger Attribute Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `toolName` | `string` | Yes | Unique name of the tool |
| `description` | `string` | No | Human-readable description of what the tool does |
| `toolProperties` | `string` (JSON) | No | JSON-serialized array of tool property definitions |
| `useResultSchema` | `bool` | No | Whether to use result schema (default: `false`) |
| `metadata` | `string` (JSON) | No | JSON-serialized metadata object for the tool |

#### Tool Properties Schema

The `toolProperties` field contains a JSON array of property definitions:

```json
[
  {
    "propertyName": "name",
    "propertyType": "string",
    "description": "The name of the person",
    "isRequired": true,
    "isArray": false,
    "enumValues": []
  },
  {
    "propertyName": "tags",
    "propertyType": "string",
    "description": "Tags for the item",
    "isRequired": false,
    "isArray": true,
    "enumValues": []
  },
  {
    "propertyName": "status",
    "propertyType": "string",
    "description": "Status of the item",
    "isRequired": true,
    "isArray": false,
    "enumValues": ["Active", "Inactive", "Pending"]
  }
]
```

##### Property Type Values

| Type | Description |
|------|-------------|
| `string` | Text values (also used for DateTime, Guid, char, enums) |
| `integer` | Integer numbers (int, Int32) |
| `number` | Floating-point numbers (double, float, decimal, etc.) |
| `boolean` | Boolean values |
| `object` | Complex objects/POCOs |

##### Array Types

Set `isArray: true` to indicate the property accepts an array of the specified type.

##### Enum Types

For enum properties:
- Set `propertyType` to `"string"`
- Populate `enumValues` with the valid enum member names

#### Binding Data Contract

The following binding data is made available during function execution:

| Key | Type | Description |
|-----|------|-------------|
| `{parameterName}` | varies | The trigger parameter value |
| `mcptoolcontext` | `ToolInvocationContext` | Full invocation context |
| `mcpsessionid` | `string` | Session ID (if available) |
| `mcptoolargs` | `IDictionary<string, string>` | Tool arguments as string dictionary |
| `$return` | `object` | Return value binding |

---

### Resource Trigger Binding

**Binding Type:** `mcpResourceTrigger`

Used to define MCP resources that can be read by MCP clients.

#### Trigger Attribute Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `uri` | `string` | Yes | Unique URI identifier for the resource (must be absolute) |
| `resourceName` | `string` | Yes | Human-readable name of the resource |
| `title` | `string` | No | Optional title for display purposes |
| `description` | `string` | No | Description of the resource |
| `mimeType` | `string` | No | MIME type of the resource content |
| `size` | `long?` | No | Optional size in bytes |
| `metadata` | `string` (JSON) | No | JSON-serialized metadata object |

#### Resource URI Requirements

- Must be an absolute URI with a scheme
- Common schemes: `file://`, `ui://`, `https://`
- Example: `file://readme.md`, `ui://my-app/widget`

#### Binding Data Contract

| Key | Type | Description |
|-----|------|-------------|
| `{parameterName}` | varies | The trigger parameter value |
| `mcpresourcecontext` | `ResourceInvocationContext` | Full invocation context |
| `mcpresourceuri` | `string` | The requested resource URI |
| `mcpsessionid` | `string` | Session ID (if available) |
| `$return` | `object` | Return value binding |

---

### Tool Property Input Binding

**Binding Type:** `mcpToolProperty`

Used to bind individual tool arguments to function parameters.

#### Attribute Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `propertyName` | `string` | Yes | Name of the property in tool arguments |
| `description` | `string` | No | Description of the property |
| `isRequired` | `bool` | No | Whether the property is required (default: `false`) |
| `propertyType` | `string` | Auto | Automatically set based on parameter type |

---

## Invocation Context Objects

### ToolInvocationContext

Passed to tool functions with full context about the invocation.

```json
{
  "name": "toolName",
  "arguments": {
    "param1": "value1",
    "param2": 42
  },
  "sessionid": "session-123",
  "clientinfo": {
    "name": "ClientApp",
    "version": "1.0.0"
  },
  "transport": {
    "name": "http-streamable",
    "sessionId": "session-123",
    "properties": {
      "headers": { "X-Custom": "value" }
    }
  }
}
```

| Field | Type | Description |
|-------|------|-------------|
| `name` | `string` | Name of the invoked tool |
| `arguments` | `object` | Dictionary of argument name to JSON element values |
| `sessionid` | `string?` | Session identifier |
| `clientinfo` | `Implementation?` | MCP client information (name, version) |
| `transport` | `Transport?` | Transport-specific information |

### ResourceInvocationContext

Passed to resource functions with context about the read request.

```json
{
  "uri": "file://readme.md",
  "sessionid": "session-123",
  "clientinfo": {
    "name": "ClientApp",
    "version": "1.0.0"
  },
  "transport": {
    "name": "http-streamable",
    "sessionId": "session-123",
    "properties": {}
  }
}
```

| Field | Type | Description |
|-------|------|-------------|
| `uri` | `string` | URI of the requested resource |
| `sessionid` | `string?` | Session identifier |
| `clientinfo` | `Implementation?` | MCP client information |
| `transport` | `Transport?` | Transport-specific information |

### Transport Object

```json
{
  "name": "http-streamable",
  "sessionId": "session-123",
  "properties": {
    "key": "value"
  }
}
```

| Field | Type | Description |
|-------|------|-------------|
| `name` | `string` | Transport type name |
| `sessionId` | `string?` | Transport-level session ID |
| `properties` | `object` | Transport-specific properties |

#### Known Transport Types

| Name | Description |
|------|-------------|
| `http-streamable` | HTTP Streamable transport |
| `http-sse` | Server-Sent Events transport |

---

## Metadata System

### Overview

Both tools and resources support custom metadata that is propagated to MCP clients via the `_meta` field in list responses.

### Metadata JSON Format

Metadata is passed as a JSON object string in the `metadata` property of trigger attributes.

```json
{
  "author": "John Doe",
  "version": 1.0,
  "ui": {
    "resourceUri": "ui://my-app/widget",
    "prefersBorder": true
  },
  "tags": ["utility", "productivity"]
}
```

### Host Metadata Parsing

The host uses `MetadataParser.ParseMetadata()` to convert the JSON string into `IReadOnlyDictionary<string, object?>`:

```csharp
IReadOnlyDictionary<string, object?> metadata = MetadataParser.ParseMetadata(metadataJsonString);
```

Supported value types:

- `null`
- `bool`
- `string`
- `long` / `double` / `decimal` (numbers)
- `Dictionary<string, object?>` (nested objects)
- `List<object?>` (arrays)

---

## Return Value Contracts

### Tool Return Values

Tools can return various content types. The host processes return values through value binders.

#### Simple Return (Default)

For simple returns (`useResultSchema: false`), the host expects any JSON-serializable value. The value will be wrapped in a text content block automatically.

#### Structured Return (useResultSchema: true)

When `useResultSchema` is enabled, return an `McpToolResult` wrapper:

```json
{
  "type": "text",
  "content": "{\"type\":\"text\",\"text\":\"Hello, world!\"}"
}
```

##### Content Types

| Type | Description |
|------|-------------|
| `text` | Text content block |
| `image` | Image content block |
| `resource` | Resource link block |
| `multiContent` | Array of mixed content blocks |

##### Multi-Content Example

```json
{
  "type": "multiContent",
  "content": "[{\"type\":\"text\",\"text\":\"Here's an image:\"},{\"type\":\"image\",\"data\":\"base64...\",\"mimeType\":\"image/png\"}]"
}
```

### Resource Return Values

Resources can return text or binary content.

#### Text Content

Return a string value. The host will wrap it in a `TextResourceContents` with the URI and MIME type from the trigger attribute.

#### Binary Content

Return a byte array. The host will base64-encode it and wrap in a `BlobResourceContents` with the URI and MIME type from the trigger attribute.

#### Structured Return

Return an `McpResourceResult` for explicit control:

```json
{
  "type": "text",
  "content": "{\"uri\":\"file://readme.md\",\"mimeType\":\"text/markdown\",\"text\":\"# Content\"}"
}
```

---

## Expected Binding Metadata Format

The host expects function bindings to be registered with the following JSON format.

### Tool Trigger Binding Example

```json
{
  "type": "mcpToolTrigger",
  "direction": "in",
  "name": "context",
  "toolName": "GetServerTime",
  "description": "Returns the current server time",
  "toolProperties": "[{\"propertyName\":\"format\",\"propertyType\":\"string\",\"description\":\"Date format\",\"isRequired\":false,\"isArray\":false,\"enumValues\":[]}]",
  "metadata": "{\"author\":\"John Doe\",\"version\":1.0}"
}
```

### Resource Trigger Binding Example

```json
{
  "type": "mcpResourceTrigger",
  "direction": "in",
  "name": "context",
  "uri": "file://readme.md",
  "resourceName": "readme",
  "description": "Application readme file",
  "mimeType": "text/plain",
  "metadata": "{\"author\":\"Jane Doe\"}"
}
```

### Tool Property Binding Example

```json
{
  "type": "mcpToolProperty",
  "direction": "in",
  "name": "userName",
  "propertyName": "userName",
  "propertyType": "string",
  "description": "The user's name",
  "isRequired": true
}
```

---

## Input Schema Generation

The host generates JSON Schema for tool input from registered properties.

### Schema Structure

```json
{
  "type": "object",
  "properties": {
    "name": {
      "type": "string",
      "description": "The name parameter"
    },
    "count": {
      "type": "integer",
      "description": "Number of items"
    },
    "tags": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "description": "List of tags"
    },
    "status": {
      "type": "string",
      "enum": ["Active", "Inactive"],
      "description": "Current status"
    }
  },
  "required": ["name", "count"]
}
```

---

## Required Property Validation

The host validates required properties before invoking tool functions.

### Validation Behavior

- Missing required properties result in `McpProtocolException` with `InvalidParams` error code
- Validation occurs before function invocation
- Error message lists all missing required properties

### Error Response

```json
{
  "error": {
    "code": -32602,
    "message": "One or more required tool properties are missing values. Please provide: name, userId"
  }
}
```

---

## List Tools/Resources Response

### ListToolsResult

```json
{
  "tools": [
    {
      "name": "GetServerTime",
      "description": "Returns the current server time",
      "inputSchema": { ... },
      "_meta": {
        "author": "John Doe",
        "version": 1.0
      }
    }
  ]
}
```

### ListResourcesResult

```json
{
  "resources": [
    {
      "uri": "file://readme.md",
      "name": "readme",
      "description": "Application readme file",
      "mimeType": "text/plain",
      "size": 1024,
      "_meta": {
        "author": "Jane Doe",
        "lastModified": "2024-01-01"
      }
    }
  ]
}
```

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-01 | Initial specification |
| 1.1 | 2025-01 | Added tool metadata support |
| 1.2 | 2025-01 | Added enum support for tool properties |

---

## Appendix A: JSON Property Names

### Binding JSON Properties

| Context | Property | JSON Name |
|---------|----------|-----------|
| Tool Trigger | Tool Name | `toolName` |
| Tool Trigger | Description | `description` |
| Tool Trigger | Properties | `toolProperties` |
| Tool Trigger | Metadata | `metadata` |
| Tool Trigger | Use Result Schema | `useResultSchema` |
| Resource Trigger | URI | `uri` |
| Resource Trigger | Name | `resourceName` |
| Resource Trigger | Description | `description` |
| Resource Trigger | MIME Type | `mimeType` |
| Resource Trigger | Size | `size` |
| Resource Trigger | Metadata | `metadata` |
| Tool Property | Name | `propertyName` |
| Tool Property | Type | `propertyType` |
| Tool Property | Description | `description` |
| Tool Property | Required | `isRequired` |
| Tool Property | Is Array | `isArray` |
| Tool Property | Enum Values | `enumValues` |

### Invocation Context JSON Properties

| Property | JSON Name |
|----------|-----------|
| Name | `name` |
| Arguments | `arguments` |
| Session ID | `sessionid` |
| Client Info | `clientinfo` |
| Transport | `transport` |
| URI | `uri` |
