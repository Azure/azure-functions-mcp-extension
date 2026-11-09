# MCP Conformance

This directory holds the configuration for running the
[Model Context Protocol conformance suite](https://github.com/modelcontextprotocol/conformance)
against the Azure Functions MCP extension.

## How it runs

The `.github/workflows/mcp-conformance.yml` workflow:

1. Builds `test/ConformanceTestApp` against the in-tree extension.
   This is a dedicated test app whose tools, resources, and prompts
   match the fixture surface the conformance scenarios expect (names
   like `test_simple_text`, URIs like `test://static-text`, etc.). The
   regular `TestAppIsolated` exposes different fixtures, so it isn't
   used here.
2. Starts Azurite and the Azure Functions host (Core Tools v4).
3. Invokes the conformance runner in server mode against the extension's
   Streamable HTTP endpoint: `http://localhost:7071/runtime/webhooks/mcp`.
4. Compares results against `conformance-baseline.yml`.

The `active` server suite is used. To run a broader set locally, swap the
`suite` input to `all` (includes pending / draft scenarios).

## Running locally

```bash
# 1. From the repo root, build the conformance app and start the host.
dotnet restore src/Microsoft.Azure.Functions.Extensions.Mcp/Extensions.Mcp.csproj
dotnet build test/ConformanceTestApp/ConformanceTestApp.csproj -c Release
cd out/bin/ConformanceTestApp/release
func start --port 7071

# 2. In a separate shell:
npx @modelcontextprotocol/conformance server \
  --url http://localhost:7071/runtime/webhooks/mcp \
  --suite active \
  --expected-failures ./eng/conformance/conformance-baseline.yml
```

## Updating the baseline

* If a newly-failing scenario is a real bug, fix it. Do not add it to
  the baseline without a tracking issue.
* If a listed scenario starts passing, remove its entry in the same PR.
* Prefer narrowing entries to a single scenario over whole suites.
