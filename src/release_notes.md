## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Extensions.Mcp 1.6.0

- Refactored token utility to use dedicated `IUriStateProtector` abstraction (#268)
- Advertise `Prompts` in `ServerCapabilities` so spec-compliant MCP clients invoke `prompts/list` (#271)
- Added `UseResultSchema` to `McpPromptTriggerAttribute` for unwrapping `McpPromptResult` envelopes (#212)

### Microsoft.Azure.Functions.Worker.Extensions.Mcp 1.6.0

- Updated to ship with host extension 1.6.0

### Microsoft.Azure.Functions.Worker.Extensions.Mcp.Sdk 1.0.0-preview.5

- Upgraded MCP C# SDK dependency from 1.2.0 to 1.4.0 (#276)
- Add support for strongly-typed prompt trigger return types: `GetPromptResult`, `PromptMessage`, and `IList<PromptMessage>` (#212)
