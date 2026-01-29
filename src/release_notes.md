## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Extensions 1.2.0

- Added feature for the extension to consume worker generated input schema if `UseWorkerInputSchema` is enabled. (#136)
- Added support for MCP Resources via `McpResourceTrigger` binding (#168)
- Added support for Resource metadata via `McpMetadata` attribute (#170)
- Added support for Tool metadata via `McpMetadata` attribute (#183)

### Microsoft.Azure.Functions.Worker.Extensions.Mcp 1.2.0-preview.1

- Added support for MCP Resources via `McpResourceTrigger` binding (#169)
- Added support for Resource metadata via `McpMetadata` attribute  (#170)
- Added support for Tool metadata via `McpMetadata` attribute  (#183)

### Microsoft.Azure.Functions.Worker.Extensions.Mcp.Sdk 1.0.0-preview.3

- Updated `Microsoft.Azure.Functions.Worker.Extensions.Mcp.Sdk` to take a dependency on `Microsoft.Azure.Functions.Worker.Extensions.Mcp` (#181)
