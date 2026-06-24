# MCP Protocol Conformance & Support

This page describes which versions of the
[Model Context Protocol](https://modelcontextprotocol.io) and which
protocol features the Azure Functions MCP extension supports today, and
how that support is verified.

## How we report support

We run the official
[MCP conformance suite](https://github.com/modelcontextprotocol/conformance)
in server mode against a real Azure Functions host loaded with this
extension. See `.github/workflows/mcp-conformance.yml`. The
authoritative answer to "does the extension support feature X" is the
latest green run of that workflow, not this table.

The table below is a human summary kept in sync with the conformance
results.

## Protocol versions

The wire-level protocol version is negotiated by the
[`ModelContextProtocol` SDK](https://www.nuget.org/packages/ModelContextProtocol)
that the extension and the .NET isolated worker SDK depend on. The
extension itself does not pin a protocol version: it advertises whatever
the bundled SDK supports during `initialize`.

| Layer                                                          | SDK package version |
| -------------------------------------------------------------- | ------------------- |
| `Microsoft.Azure.Functions.Extensions.Mcp` (host)              | `0.4.0-preview.3`   |
| `Microsoft.Azure.Functions.Worker.Extensions.Mcp.Sdk` (worker) | `1.2.0`             |

Clients can discover the negotiated `protocolVersion` and the server's
declared `capabilities` from the standard MCP `initialize` response.

## Feature support

| Feature                     | Supported | Notes                                                                            |
| --------------------------- | --------- | -------------------------------------------------------------------------------- |
| Tools                       | Yes       | Trigger-based, with typed argument binding                                       |
| Resources (static)          | Yes       |                                                                                  |
| Resource templates          | Yes       |                                                                                  |
| Prompts                     | Yes       |                                                                                  |
| Notifications (listChanged) | Yes       |                                                                                  |
| Transport: SSE              | Yes       | `/runtime/webhooks/mcp/sse`                                                      |
| Transport: Streamable HTTP  | Yes       | `/runtime/webhooks/mcp` (default)                                                |
| Sampling (server-initiated) | No        | Not currently surfaced through the trigger model                                 |
| Roots                       | No        | Not currently surfaced through the trigger model                                 |
| Auth (OAuth)                | n/a       | Out of scope for the extension; handled by the Functions host, APIM, Easy Auth   |

For the exact list of conformance scenarios that pass and any expected
failures, see [`eng/conformance/conformance-baseline.yml`](../eng/conformance/conformance-baseline.yml)
and the latest run of the `MCP Conformance` workflow.

## Running conformance locally

See [`eng/conformance/README.md`](../eng/conformance/README.md).
