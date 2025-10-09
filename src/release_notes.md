## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Extensions 1.0.0-preview.8

- Enforcing required properties in MCP Extension (#105)
- Built-in array support (#117)

### Microsoft.Azure.Functions.Worker.Extensions.Mcp 1.0.0-preview.8

- Removing the need to call `EnableMcpToolMetadata` (#102)
- Changing IOptionsMonitor to IOptionsSnapshot to fix bug with DI service not resolving (#101)
- Implicitly typed MCP properties (#117)
- Built-in array support/handling (#117)
- Enhancements to property type mapping and handling (#117)

#### Breaking Changes
- The `EnableMcpToolMetadata` API is no longer needed and has been removed (#102)

