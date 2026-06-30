## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Extensions <version>

- Advertise `Prompts` in `ServerCapabilities` so spec-compliant MCP clients invoke `prompts/list`. (#271)

### Microsoft.Azure.Functions.Worker.Extensions.Mcp <version>

- <entry>

### Microsoft.Azure.Functions.Worker.Extensions.Mcp.Sdk <version>

- Upgraded MCP C# SDK dependency to 1.4.0 (#276)
- Add support for strongly-typed prompt trigger return types: `GetPromptResult`, `PromptMessage`, and `IList<PromptMessage>` (#212)
