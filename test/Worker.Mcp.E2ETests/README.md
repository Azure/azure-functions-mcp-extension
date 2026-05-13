# Worker.Mcp.E2ETests

## Charter

This project hosts end-to-end tests that boot a real Azure Functions host (via
Core Tools) and drive it through the MCP protocol over HTTP. E2E runs are
slow and flaky-prone, so the suite is intentionally kept small.

E2E tests exist to validate:

1. **The Functions hosting model.** The extension loads, the host starts, and
   tools/resources/prompts are discoverable end-to-end.
2. **Transport plumbing.** Each supported transport (SSE, Streamable HTTP)
   produces a working session.
3. **Happy-path integration for each MCP primitive.** One representative
   call per primitive (tool / prompt / resource / template) confirms the full
   request/response pipeline is wired up.
4. **End-to-end error paths.** Unknown tool, missing required argument,
   non-existent resource. These require the host to report the error through
   the protocol shape clients depend on.

Everything else belongs in unit tests:

- Tool / prompt / resource **metadata content** (titles, descriptions,
  schemas, `_meta` blobs) is built by the metadata pipeline. Assert it in
  `Worker.Extensions.Mcp.Tests` (builder, metadata, schema, serialization
  tests) and `Extensions.Mcp.Tests` (registry tests).
- **Argument conversion** (typed, collection, POCO, Guid/DateTime) is
  covered by the converter unit tests in `Worker.Extensions.Mcp.Tests`.
- **Return-value binding** (text, image, resource link, structured,
  multi-content) is covered by the binder unit tests in
  `Extensions.Mcp.Tests`.
- **Schema validation** and **required-argument enforcement** are covered by
  `McpToolSchemaValidatorTests` and the registry tests.

## Transport matrix

Use the shared `TransportModes` `TheoryData` instead of repeating
`[InlineData(HttpTransportMode.Sse)] ...` lines. `AutoDetect` currently
resolves to the same SSE endpoint as `Sse` in `McpEndToEndFixtureBase`, so it
is deliberately excluded from the default matrix to avoid duplicate runs.
Add an explicit `[InlineData(HttpTransportMode.AutoDetect)]` test only when
the behaviour under test depends on the auto-detection code path itself.

```csharp
[Theory]
[MemberData(nameof(TransportModes.All), MemberType = typeof(TransportModes))]
public async Task MyTest(HttpTransportMode mode) { ... }
```

## Adding tests

Before adding a new e2e test, ask:

1. Can this be a unit test against the metadata pipeline, a converter, a
   binder, or a registry? If yes, write it there instead.
2. Is it asserting protocol-envelope shape (jsonrpc/result/error)? The MCP
   SDK already guarantees this. Don't re-assert it.
3. Is it exercising the host / transport / a primitive end-to-end? If yes,
   add it here. Keep it focused on one concern; don't pile multiple
   assertions per request.
