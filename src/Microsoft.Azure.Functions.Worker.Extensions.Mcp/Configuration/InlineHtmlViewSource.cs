// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;

/// <summary>
/// A view source backed by inline HTML. For testing only.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record InlineHtmlViewSource(string Html) : McpViewSource;

/// <summary>
/// Test-only extensions for McpViewSource. Not intended for production use.
/// </summary>
public static class McpViewSourceTestExtensions
{
    /// <summary>
    /// Creates a view source from inline HTML. Intended for testing only.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete(
        "FromHtml is intended for testing only. Use McpViewSource.FromFile or " +
        "McpViewSource.FromEmbeddedResource in production code.",
        DiagnosticId = "MCP001")]
    public static McpViewSource FromHtml(string html)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(html);
        return new InlineHtmlViewSource(html);
    }
}
