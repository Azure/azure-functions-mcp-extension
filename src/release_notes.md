## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Extensions 1.0.0-preview.7

- Adding configurable support for absolute URIs in endpoint message.
- Adding instrumentation to emit a server span for the tools/call. (#47)
- Add support for configuring required properties in tool metadata. (#54)
  NOTE: This introduces a change in behavior as existing properties will no longer be required by default.

### Microsoft.Azure.Functions.Worker.Extensions.Mcp 1.0.0-preview.7

- Add support for configuring required properties in tool metadata. (#54)
- Add support for POCO bindings. (#77)
- Add support for binding to `ToolInvocationContext` without a binding attribute (#83)
- Nullable properties on a `McpToolPropertyAttribute` now correctly return null when not set. (#83)

#### Breaking Changes

- The values in the `ToolInvocationContext.Arguments` dictionary is now returning the resolved types instead of JsonValueKind. (#83)
