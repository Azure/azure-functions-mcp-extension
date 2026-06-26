## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Extensions <version>

- Added `UseResultSchema` to `McpPromptTriggerAttribute`. When set by the worker, the host unwraps the `McpPromptResult` envelope produced by the worker instead of inferring the shape from the JSON. (#212)
- Advertise `Prompts` in `ServerCapabilities` so spec-compliant MCP clients invoke `prompts/list`.

### Microsoft.Azure.Functions.Worker.Extensions.Mcp 1.5.1

- Fixed argument conversion so tool properties typed as arrays of complex objects (and POCO trigger properties typed as arrays of complex objects, or as nested complex objects) are populated instead of arriving as `null` elements. (#260)

### Microsoft.Azure.Functions.Worker.Extensions.Mcp.Sdk <version>

- Upgraded MCP C# SDK dependency to 1.4.0 (#276)
- Add support for strongly-typed prompt trigger return types: `GetPromptResult`, `PromptMessage`, and `IList<PromptMessage>` (#212)
