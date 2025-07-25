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
