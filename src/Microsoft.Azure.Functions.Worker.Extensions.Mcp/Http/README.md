# StreamableHttp Support for Azure Functions Isolated Worker

This directory contains the StreamableHttp transport implementation for the Azure Functions Worker Extensions MCP library.

## Overview

StreamableHttp transport enables bidirectional communication over HTTP using Server-Sent Events (SSE) and HTTP streaming, allowing MCP clients and servers to maintain persistent connections within the Azure Functions isolated worker process.

## Components

### Core Files

- **`IStreamableHttpRequestHandler.cs`** - Interface for handling StreamableHttp requests
- **`StreamableHttpRequestHandler.cs`** - Main implementation of the StreamableHttp protocol handler
- **`ISseRequestHandler.cs`** - Interface for Server-Sent Events request handling  
- **`SseRequestHandler.cs`** - Implementation for SSE request handling (stub)

### Session Management

- **`IMcpClientSession.cs`** - Interface for MCP client session management
- **`SessionManager.cs`** - Complete session management implementation including:
  - `IMcpClientSessionManager<T>` interface for transport-specific session management
  - `DefaultMcpClientSession` implementation for managing individual sessions
  - Session lifecycle management and cleanup

## Key Features

1. **HTTP Context Integration**: Seamless integration with ASP.NET Core HTTP context in isolated worker processes
2. **Stateless Session Support**: Handles both stateful and stateless session scenarios
3. **Media Type Validation**: Validates `application/vnd.mcp` content type for MCP protocol compliance
4. **Duplex Pipe Handling**: Manages bidirectional streaming using System.IO.Pipelines
5. **Error Handling**: Comprehensive JSON-RPC error handling with proper HTTP status codes
6. **Dependency Injection**: Full integration with Microsoft.Extensions.DependencyInjection

## Configuration

StreamableHttp support is **automatically enabled** when you reference this package - no explicit configuration needed!

```csharp
var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();
// StreamableHttp transport works automatically!
builder.Build().Run();
```

### Optional: Custom Configuration

For custom configuration, you can still explicitly call:

```csharp
builder.EnableMcp(options => {
    options.EnableStreamableHttp = true; // Default: true (SSE is deprecated)
    options.EncryptClientState = false; // Default: false
});
```

### Tool Properties Support

If you're using `[McpToolProperty]` attributes, you need to explicitly enable metadata processing:

```csharp
// Auto-registered transport works without this
builder.EnableMcpToolMetadata(); // Required for tool property attributes
```

### Legacy Configuration

For backward compatibility, you can still use:

```csharp
builder.EnableMcpStreamableHttp(options => {
    // Configure MCP options
});
```

### Disabling StreamableHttp (Not Recommended)

If you need to disable StreamableHttp and use only SSE (deprecated):

```csharp
builder.EnableMcp()
      .DisableStreamableHttp(); // Forces SSE transport only
```

### Suppressing MCP Endpoints from Function List

MCP infrastructure endpoints (`mcp-streamable`, `mcp-sse`, `mcp-message`) will appear in the Functions host startup list. This is cosmetic only - the endpoints remain fully functional.

**Note**: Previous endpoint suppression functionality was removed due to circular dependency issues that caused runtime crashes. The endpoints are now always visible in the startup list but this does not affect functionality.

## Usage

The StreamableHttp handler is designed to work within Azure Functions HTTP triggers:

```csharp
[Function("McpStreamableHttp")]
public async Task<IActionResult> HandleStreamableHttp(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mcp/streamablehttp")] HttpRequest req)
{
    var handler = serviceProvider.GetRequiredService<IStreamableHttpRequestHandler>();
    await handler.HandleRequestAsync(req.HttpContext);
    return new EmptyResult();
}
```

## Architecture

The implementation follows the same architectural patterns as the main Extensions.Mcp project but adapted for the isolated worker process:

- **Transport Layer**: Uses `StreamableHttpTransport` from ModelContextProtocol package
- **Session Management**: Manages MCP server instances and client sessions
- **Protocol Handling**: Implements MCP protocol over HTTP streaming
- **Error Handling**: Provides JSON-RPC compliant error responses

## Dependencies

- **ModelContextProtocol** (v0.3.0-preview.3) - Core MCP protocol implementation
- **System.Net.ServerSentEvents** (v10.0.0-preview.6.25358.103) - SSE support
- **Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore** (v1.3.2) - HTTP context integration

## Limitations

- Server creation is simplified in the current implementation
- Full MCP server lifecycle management requires additional integration
- SSE handler is currently a stub implementation

## Future Enhancements

- Complete MCP server factory integration
- Enhanced error logging and diagnostics
- Full SSE request handler implementation
- Connection pooling and performance optimizations
