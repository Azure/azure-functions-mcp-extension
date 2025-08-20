# StreamableHttp Auto-Registration

## Overview

**StreamableHttp transport is automatically enabled** when you reference the Azure Functions MCP extension package. No explicit configuration is required for basic MCP functionality, but tool properties require explicit metadata provider registration.

## Current Architecture Summary

- **Auto-Registration**: MCP transport works immediately upon package reference
- **StreamableHttp Default**: Modern transport enabled by default (SSE is deprecated)
- **Tool Properties**: Require explicit `EnableMcpToolMetadata()` call due to metadata provider timing issues
- **Zero-Config Experience**: Basic MCP functionality "just works"

## Usage Patterns

### Automatic (Recommended for Basic MCP)

```csharp
var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();
// MCP transport works automatically - no configuration needed!
builder.Build().Run();
```

### With Tool Properties

```csharp
var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();

// Required for [McpToolProperty] attributes
builder.EnableMcpToolMetadata();

builder.Build().Run();
```

### Custom Configuration (Optional)

```csharp
builder.EnableMcp(options => {
    options.EnableStreamableHttp = true; // Default: true (recommended)
    options.EncryptClientState = false; // Default: false
});
```

### Disable StreamableHttp (Not Recommended)

If you need to use only SSE transport (deprecated):

```csharp
builder.EnableMcp(options => {
    options.EnableStreamableHttp = false; // Forces SSE-only mode
});

// Or using the fluent API:
builder.EnableMcp()
      .DisableStreamableHttp();
```

### Legacy Methods (Backward Compatible)

```csharp
// These still work as before:
builder.EnableMcpStreamableHttp(); // Explicitly enables StreamableHttp
builder.EnableMcpToolMetadata();   // Enables tool metadata processing
```

## Configuration Options

### McpOptions Properties

| Property | Default | Description |
|----------|---------|-------------|
| `EnableStreamableHttp` | `true` | Enables StreamableHttp transport (recommended) |
| `EncryptClientState` | `false` | Enables client state encryption |
| `MessageOptions` | `new()` | Message handling configuration |

### Extension Methods

| Method | Description | When to Use |
|--------|-------------|-------------|
| *(Automatic)* | **Recommended**: MCP works without any calls | Basic MCP transport only |
| `EnableMcpToolMetadata()` | **Required**: Enables `[McpToolProperty]` processing | When using tool property attributes |
| `EnableMcp(Action<McpOptions>)` | **Optional**: Custom configuration | Advanced configuration needed |
| `DisableStreamableHttp()` | **Not Recommended**: Forces SSE-only | Legacy compatibility only |

## Auto-Registration Architecture

### How It Works

1. **Assembly-level Registration**: `[assembly: WorkerExtensionStartup(typeof(McpExtensionStartup))]`
2. **Automatic Service Registration**: `McpExtensionStartup.Configure()` calls `EnableMcp()` automatically
3. **Transport Layer**: StreamableHttp services registered by default
4. **Zero Configuration**: Works immediately upon NuGet package reference

### Why Tool Properties Require Explicit Call

Due to Azure Functions runtime timing, the metadata provider must be registered after the base `IFunctionMetadataProvider` is available. Auto-startup happens too early, so tool property support requires explicit `EnableMcpToolMetadata()` registration.

## Transport Comparison

| Feature | StreamableHttp | SSE (Deprecated) |
|---------|---------------|------------------|
| **Bidirectional** | ✅ Full duplex | ❌ Server-to-client only |
| **Connection Efficiency** | ✅ Single HTTP connection | ❌ Separate connections needed |
| **Azure Functions Support** | ✅ Optimized | ⚠️ Limited |
| **MCP Spec Status** | ✅ Current | ❌ Deprecated |
| **Future Support** | ✅ Ongoing | ❌ Phase-out planned |
| **Auto-Registration** | ✅ Works automatically | ✅ Works automatically |

## Troubleshooting

### Issue: Tool properties not working
**Solution**: Add `builder.EnableMcpToolMetadata()` after `ConfigureFunctionsWebApplication()`

### Issue: Need SSE-only mode
**Solution**: Use `DisableStreamableHttp()` or set `options.EnableStreamableHttp = false`

### Issue: MCP endpoints showing in startup list
**Solution**: This is expected behavior - endpoints are visible but this is cosmetic only

### Issue: Stack overflow during startup
**Solution**: Ensure you're not calling `EnableMcp()` explicitly if using auto-registration (avoid double registration)

## Testing

All 93 existing tests pass, ensuring stability while providing the zero-configuration experience.

## Implementation Evolution

The implementation evolved through several phases:

1. **Manual Configuration**: Originally required explicit `EnableMcp()` calls
2. **Endpoint Suppression**: Attempted to hide infrastructure endpoints (caused stack overflow)
3. **Stack Overflow Resolution**: Removed problematic metadata decorators
4. **Auto-Registration**: Enabled seamless experience with `WorkerExtensionStartup`
5. **Tool Properties Fix**: Separated metadata provider registration due to timing issues

This provides the optimal balance of "zero-config for transport" and "explicit config for advanced features".
