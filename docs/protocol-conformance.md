# MCP Protocol Conformance & Support

This page describes which versions of the
[Model Context Protocol](https://modelcontextprotocol.io) and which
protocol features the Azure Functions MCP extension supports today, and
how that support is verified.

## How we report support

We run the official
[MCP conformance suite](https://github.com/modelcontextprotocol/conformance)
in server mode against a real Azure Functions host loaded with this
extension. See `.github/workflows/mcp-conformance.yml`.

The table below uses the exact scenario names from the conformance suite.
CI verifies this table matches the latest results — if a scenario
changes status, the workflow will flag this doc as out of date and the
PR comment will include a reminder to update it.

## Protocol versions

The wire-level protocol version is negotiated by the
[`ModelContextProtocol` SDK](https://www.nuget.org/packages/ModelContextProtocol)
that the extension and the .NET isolated worker SDK depend on. The
extension itself does not pin a protocol version: it advertises whatever
the bundled SDK supports during `initialize`.

<!-- conformance-protocol-version: 2025-06-18 -->
**Supported MCP protocol version:** `2025-06-18`

| Layer                                                          | SDK package version |
| -------------------------------------------------------------- | ------------------- |
| `Microsoft.Azure.Functions.Extensions.Mcp` (host)              | `0.4.0-preview.3`   |
| `Microsoft.Azure.Functions.Worker.Extensions.Mcp.Sdk` (worker) | `1.2.0`             |

Clients can discover the negotiated `protocolVersion` and the server's
declared `capabilities` from the standard MCP `initialize` response.

## Conformance scenarios

<!-- conformance-table-start -->
| Status | Scenario | Detail |
| --- | --- | --- |
| ✅ | `server-initialize` | 1 passed, 0 failed |
| ✅ | `logging-set-level` | 1 passed, 0 failed |
| ✅ | `ping` | 1 passed, 0 failed |
| ❌ | `completion-complete` | 0 passed, 1 failed |
| ✅ | `tools-list` | 1 passed, 0 failed |
| ✅ | `tools-call-simple-text` | 1 passed, 0 failed |
| ✅ | `tools-call-image` | 1 passed, 0 failed |
| ✅ | `tools-call-audio` | 1 passed, 0 failed |
| ✅ | `tools-call-embedded-resource` | 1 passed, 0 failed |
| ✅ | `tools-call-mixed-content` | 1 passed, 0 failed |
| ❌ | `tools-call-with-logging` | 0 passed, 1 failed |
| ✅ | `tools-call-error` | 1 passed, 0 failed |
| ❌ | `tools-call-with-progress` | 0 passed, 1 failed |
| ❌ | `tools-call-sampling` | 0 passed, 1 failed |
| ❌ | `tools-call-elicitation` | 0 passed, 1 failed |
| ❌ | `elicitation-sep1034-defaults` | 0 passed, 1 failed |
| ✅ | `server-sse-multiple-streams` | 2 passed, 0 failed |
| ❌ | `elicitation-sep1330-enums` | 0 passed, 1 failed |
| ✅ | `resources-list` | 1 passed, 0 failed |
| ✅ | `resources-read-text` | 1 passed, 0 failed |
| ✅ | `resources-read-binary` | 1 passed, 0 failed |
| ✅ | `resources-templates-read` | 1 passed, 0 failed |
| ✅ | `resources-subscribe` | 1 passed, 0 failed |
| ✅ | `resources-unsubscribe` | 1 passed, 0 failed |
| ✅ | `prompts-list` | 1 passed, 0 failed |
| ✅ | `prompts-get-simple` | 1 passed, 0 failed |
| ✅ | `prompts-get-with-args` | 1 passed, 0 failed |
| ✅ | `prompts-get-embedded-resource` | 1 passed, 0 failed |
| ✅ | `prompts-get-with-image` | 1 passed, 0 failed |
| ❌ | `dns-rebinding-protection` | 1 passed, 1 failed |
<!-- conformance-table-end -->

**24** passed, **8** failed out of **32** scenarios

Expected failures are tracked in
[`eng/conformance/conformance-baseline.yml`](../eng/conformance/conformance-baseline.yml).

## Additional features (not covered by conformance suite)

| Feature | Status | Notes |
| --- | --- | --- |
| Transport: SSE | ✅ Supported | `/runtime/webhooks/mcp/sse` |
| Transport: Streamable HTTP | ✅ Supported | `/runtime/webhooks/mcp` (default) |
| Notifications (listChanged) | ✅ Supported | |
| Auth (OAuth) | n/a | Out of scope; handled by Functions host, APIM, Easy Auth |

## Running conformance locally

See [`eng/conformance/README.md`](../eng/conformance/README.md).
