## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Extensions <version>

- Added feature for the extension to consume worker generated input schema if `UseWorkerInputSchema` is enabled. (#136)

### Microsoft.Azure.Functions.Worker.Extensions.Mcp <version>

- <entry>

### Microsoft.Azure.Functions.Worker.Extensions.Mcp.Sdk 1.0.0-preview.2

- Fixed MCP tool functions with output bindings not working correctly. Return values are no longer wrapped when output bindings are present. (#174)
  - This means that complex tool result types (ContentBlocks) are not supported when also using an output binding for now.
