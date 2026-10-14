# ConformanceTestApp

Dedicated Azure Functions isolated-worker app whose tools, resources,
and prompts implement the exact fixture surface required by the
[MCP conformance suite](https://github.com/modelcontextprotocol/conformance).

This app exists only to drive the conformance suite. The names, URIs,
and return shapes here are dictated by the scenarios in
`src/scenarios/server/*.ts` in that repo. Do not refactor them to match
the rest of the codebase's conventions.

The conformance workflow in `.github/workflows/mcp-conformance.yml`
builds this app, starts it under Core Tools, and runs the conformance
runner against the resulting Streamable HTTP endpoint.

If a scenario fails, first check whether the fixture this app exposes
matches what the scenario expects (names, return content blocks).
If yes, it's a real bug in the extension. If the scenario is for a
feature the extension does not implement (sampling, elicitation,
progress, completion, etc.), add it to `eng/conformance/conformance-baseline.yml`
with a link to a tracking issue rather than implementing a half-working
fixture here.
